/**
 * ZipGrade Clone - Client-Side Optical Mark Recognition (OMR) Engine
 * High-performance pure Canvas image processing, fiducial alignment,
 * homography perspective rectification, bubble fill ratio analysis, and visual grading overlay.
 */

window.ZipGradeOmr = {
    // Canonical rectified dimensions (A4 portrait standard ratio 1:1.414)
    CANONICAL_WIDTH: 1000,
    CANONICAL_HEIGHT: 1414,

    /**
     * Compute normalized coordinate layouts for 10, 20, 50, and 100 question bubble sheets.
     */
    getLayoutConfig: function (questionCount, optionsCount) {
        optionsCount = optionsCount || 4; // default A-D
        questionCount = questionCount || 20;

        // Fiducial corner targets (centers in relative 0..1 coordinates)
        const fiducials = {
            tl: { x: 0.06, y: 0.045 },
            tr: { x: 0.94, y: 0.045 },
            bl: { x: 0.06, y: 0.955 },
            br: { x: 0.94, y: 0.955 },
            size: 0.035
        };

        // Student ID Grid: 5 columns, digits 0-9
        // Located in upper right area below header
        const studentIdGrid = {
            startX: 0.62,
            startY: 0.11,
            colWidth: 0.055,
            rowHeight: 0.024,
            columns: 5,
            digits: 10,
            radius: 0.010
        };

        // Key Version selector: 4 bubbles (A, B, C, D)
        const versionSelector = {
            startX: 0.12,
            startY: 0.28,
            spacingX: 0.06,
            options: ['A', 'B', 'C', 'D'],
            radius: 0.011
        };

        // Questions Bubble Grid
        // Varies by question count:
        // 10 Qs: 1 column of 10 rows
        // 20 Qs: 2 columns of 10 rows
        // 50 Qs: 2 columns of 25 rows
        // 100 Qs: 4 columns of 25 rows
        let columnsCount = 2;
        let rowsPerColumn = 10;
        let startY = 0.35;
        let rowHeight = 0.027;

        if (questionCount <= 10) {
            columnsCount = 1;
            rowsPerColumn = 10;
            startY = 0.36;
            rowHeight = 0.045;
        } else if (questionCount <= 20) {
            columnsCount = 2;
            rowsPerColumn = 10;
            startY = 0.36;
            rowHeight = 0.045;
        } else if (questionCount <= 50) {
            columnsCount = 2;
            rowsPerColumn = 25;
            startY = 0.35;
            rowHeight = 0.022;
        } else { // 100
            columnsCount = 4;
            rowsPerColumn = 25;
            startY = 0.35;
            rowHeight = 0.022;
        }

        const columnWidth = 0.84 / columnsCount;
        const bubbleSpacingX = Math.min(0.045, (columnWidth * 0.65) / optionsCount);
        const bubbleRadius = (questionCount > 20) ? 0.0085 : 0.012;

        return {
            fiducials: fiducials,
            studentIdGrid: studentIdGrid,
            versionSelector: versionSelector,
            questions: {
                total: questionCount,
                optionsCount: optionsCount,
                columnsCount: columnsCount,
                rowsPerColumn: rowsPerColumn,
                startX: 0.08,
                startY: startY,
                columnWidth: columnWidth,
                rowHeight: rowHeight,
                bubbleSpacingX: bubbleSpacingX,
                bubbleRadius: bubbleRadius
            }
        };
    },

    /**
     * Process an image (from dataUrl or Image) with OMR analysis and scoring overlay.
     */
    processSheetImage: async function (imageDataUrl, questionCount, optionsCount, keyAnswersMap, customCorners) {
        const self = this;
        return new Promise((resolve, reject) => {
            const img = new Image();
            img.onload = function () {
                try {
                    const result = self.executeOmrPipeline(img, questionCount, optionsCount, keyAnswersMap, customCorners);
                    resolve(result);
                } catch (err) {
                    reject(err.toString());
                }
            };
            img.onerror = function () {
                reject("Failed to load source image for OMR processing.");
            };
            img.src = imageDataUrl;
        });
    },

    /**
     * Executes the 5-stage OMR pipeline on an HTML Image.
     */
    executeOmrPipeline: function (img, questionCount, optionsCount, keyAnswersMap, customCorners) {
        const layout = this.getLayoutConfig(questionCount, optionsCount);
        const w = img.naturalWidth || img.width;
        const h = img.naturalHeight || img.height;

        // Stage 1: Offscreen canvas for raw image & corner detection
        const rawCanvas = document.createElement('canvas');
        rawCanvas.width = w;
        rawCanvas.height = h;
        const rawCtx = rawCanvas.getContext('2d', { willReadFrequently: true });
        rawCtx.drawImage(img, 0, 0);

        // Stage 2: Corner localization
        let corners = customCorners;
        let detectedAuto = false;

        if (!corners || corners.length !== 4) {
            corners = this.detectFiducialCorners(rawCtx, w, h);
            if (corners) {
                detectedAuto = true;
            } else {
                // Fallback corners if not detected
                corners = [
                    { x: w * 0.06, y: h * 0.045 }, // Top-Left
                    { x: w * 0.94, y: h * 0.045 }, // Top-Right
                    { x: w * 0.94, y: h * 0.955 }, // Bottom-Right
                    { x: w * 0.06, y: h * 0.955 }  // Bottom-Left
                ];
            }
        }

        // Stage 3: Perspective Homography Warp to canonical matrix
        const warpCanvas = document.createElement('canvas');
        warpCanvas.width = this.CANONICAL_WIDTH;
        warpCanvas.height = this.CANONICAL_HEIGHT;
        const warpCtx = warpCanvas.getContext('2d', { willReadFrequently: true });

        this.warpPerspective(rawCanvas, warpCtx, corners, this.CANONICAL_WIDTH, this.CANONICAL_HEIGHT);

        // Stage 4: Sample Bubbles on Rectified Canvas
        const warpImgData = warpCtx.getImageData(0, 0, this.CANONICAL_WIDTH, this.CANONICAL_HEIGHT);
        const grayscale = this.toGrayscale(warpImgData);

        // 4A: Sample Key Version
        const versionResult = this.sampleVersion(grayscale, layout.versionSelector);

        // 4B: Sample Student ID Grid
        const studentIdResult = this.sampleStudentId(grayscale, layout.studentIdGrid);

        // 4C: Sample Questions Bubbles
        const answersResult = this.sampleQuestions(grayscale, layout.questions);

        // Stage 5: Draw Visual Grading Overlay on the Rectified Canvas
        this.renderGradingOverlay(warpCtx, layout, answersResult, keyAnswersMap, studentIdResult, versionResult);

        const rectifiedDataUrl = warpCanvas.toDataURL('image/jpeg', 0.88);

        return {
            detectedStudentId: studentIdResult.studentId,
            detectedVersion: versionResult.version,
            studentIdConfidence: studentIdResult.confidence,
            detectedAnswers: answersResult.answers, // map: qNumber -> ["A"]
            detectedCorners: corners,
            detectedAuto: detectedAuto,
            rectifiedImageUrl: rectifiedDataUrl,
            bubbleFillDetails: answersResult.details,
            requiresReview: studentIdResult.hasAmbiguity || answersResult.hasMultiMarks
        };
    },

    /**
     * Converts ImageData to 1-channel Grayscale Array with contrast stretching.
     */
    toGrayscale: function (imgData) {
        const w = imgData.width;
        const h = imgData.height;
        const data = imgData.data;
        const gray = new Uint8Array(w * h);

        let minVal = 255;
        let maxVal = 0;

        for (let i = 0, p = 0; i < data.length; i += 4, p++) {
            // Luminance formula
            const val = (data[i] * 299 + data[i + 1] * 587 + data[i + 2] * 114) / 1000 | 0;
            gray[p] = val;
            if (val < minVal) minVal = val;
            if (val > maxVal) maxVal = val;
        }

        return {
            width: w,
            height: h,
            data: gray,
            min: minVal,
            max: maxVal
        };
    },

    /**
     * Detects 4 fiducial black corner squares using quadrant darkness centroid search.
     */
    detectFiducialCorners: function (ctx, w, h) {
        try {
            const quadW = Math.floor(w * 0.25);
            const quadH = Math.floor(h * 0.20);

            // Quadrants: TL, TR, BR, BL
            const tl = this.findDarkestSquareInRegion(ctx, 0, 0, quadW, quadH);
            const tr = this.findDarkestSquareInRegion(ctx, w - quadW, 0, quadW, quadH);
            const br = this.findDarkestSquareInRegion(ctx, w - quadW, h - quadH, quadW, quadH);
            const bl = this.findDarkestSquareInRegion(ctx, 0, h - quadH, quadW, quadH);

            if (tl && tr && br && bl) {
                return [tl, tr, br, bl];
            }
        } catch (e) {
            console.warn("Fiducial corner detection fallback:", e);
        }
        return null;
    },

    /**
     * Searches a bounding box for the darkest solid square pattern.
     */
    findDarkestSquareInRegion: function (ctx, rx, ry, rw, rh) {
        const imgData = ctx.getImageData(rx, ry, rw, rh);
        const data = imgData.data;

        let bestScore = Infinity;
        let bestX = -1;
        let bestY = -1;

        const step = 6;
        const testSize = Math.max(12, Math.floor(rw * 0.16));

        for (let y = testSize; y < rh - testSize; y += step) {
            for (let x = testSize; x < rw - testSize; x += step) {
                let darkSum = 0;
                let count = 0;

                for (let dy = -testSize / 2; dy <= testSize / 2; dy += 4) {
                    for (let dx = -testSize / 2; dx <= testSize / 2; dx += 4) {
                        const px = Math.floor(x + dx);
                        const py = Math.floor(y + dy);
                        const idx = (py * rw + px) * 4;
                        const lum = (data[idx] + data[idx + 1] + data[idx + 2]) / 3;
                        darkSum += lum;
                        count++;
                    }
                }

                const avgLum = darkSum / count;
                if (avgLum < bestScore) {
                    bestScore = avgLum;
                    bestX = rx + x;
                    bestY = ry + y;
                }
            }
        }

        // Only accept if sufficiently dark (contrast against paper)
        if (bestScore < 140 && bestX !== -1) {
            return { x: bestX, y: bestY };
        }
        return null;
    },

    /**
     * Homography perspective warp using piecewise triangle rasterization on HTML5 Canvas.
     */
    warpPerspective: function (srcCanvas, dstCtx, corners, dstW, dstH) {
        // [tl, tr, br, bl]
        const p0 = corners[0]; // TL
        const p1 = corners[1]; // TR
        const p2 = corners[2]; // BR
        const p3 = corners[3]; // BL

        // Warp two triangles (TL-TR-BL and TR-BR-BL)
        this.warpTriangle(srcCanvas, dstCtx,
            p0, p1, p3,
            { x: dstW * 0.06, y: dstH * 0.045 },
            { x: dstW * 0.94, y: dstH * 0.045 },
            { x: dstW * 0.06, y: dstH * 0.955 }
        );

        this.warpTriangle(srcCanvas, dstCtx,
            p1, p2, p3,
            { x: dstW * 0.94, y: dstH * 0.045 },
            { x: dstW * 0.94, y: dstH * 0.955 },
            { x: dstW * 0.06, y: dstH * 0.955 }
        );
    },

    warpTriangle: function (src, dstCtx, s0, s1, s2, d0, d1, d2) {
        dstCtx.save();
        dstCtx.beginPath();
        dstCtx.moveTo(d0.x, d0.y);
        dstCtx.lineTo(d1.x, d1.y);
        dstCtx.lineTo(d2.x, d2.y);
        dstCtx.closePath();
        dstCtx.clip();

        // Calculate affine transform matrix mapping s0, s1, s2 to d0, d1, d2
        const delta = (s0.x - s2.x) * (s1.y - s2.y) - (s1.x - s2.x) * (s0.y - s2.y);
        if (Math.abs(delta) > 0.0001) {
            const a = ((d0.x - d2.x) * (s1.y - s2.y) - (d1.x - d2.x) * (s0.y - s2.y)) / delta;
            const b = ((d0.y - d2.y) * (s1.y - s2.y) - (d1.y - d2.y) * (s0.y - s2.y)) / delta;
            const c = ((d1.x - d2.x) * (s0.x - s2.x) - (d0.x - d2.x) * (s1.x - s2.x)) / delta;
            const d = ((d1.y - d2.y) * (s0.x - s2.x) - (d0.y - d2.y) * (s1.x - s2.x)) / delta;
            const e = d0.x - a * s0.x - c * s0.y;
            const f = d0.y - b * s0.x - d * s0.y;

            dstCtx.transform(a, b, c, d, e, f);
            dstCtx.drawImage(src, 0, 0);
        }

        dstCtx.restore();
    },

    /**
     * Measures the darkness fill ratio of a circular bubble ROI.
     */
    measureBubbleFill: function (grayImg, cx, cy, radius) {
        const w = grayImg.width;
        const h = grayImg.height;
        const data = grayImg.data;

        const px = Math.round(cx);
        const py = Math.round(cy);
        const r = Math.round(radius);

        let darkCount = 0;
        let totalCount = 0;
        let totalLum = 0;

        // Sample concentric rings inside the bubble (excluding outer black circle line)
        const innerRadius = Math.max(1, r - 2);

        for (let dy = -innerRadius; dy <= innerRadius; dy++) {
            for (let dx = -innerRadius; dx <= innerRadius; dx++) {
                if (dx * dx + dy * dy <= innerRadius * innerRadius) {
                    const sx = px + dx;
                    const sy = py + dy;
                    if (sx >= 0 && sx < w && sy >= 0 && sy < h) {
                        const lum = data[sy * w + sx];
                        totalLum += lum;
                        if (lum < 155) { // Dark pencil/pen mark
                            darkCount++;
                        }
                        totalCount++;
                    }
                }
            }
        }

        const avgLum = totalCount > 0 ? (totalLum / totalCount) : 255;
        const fillRatio = totalCount > 0 ? (darkCount / totalCount) : 0;

        return {
            fillRatio: fillRatio,
            avgLuminance: avgLum,
            isMarked: (fillRatio >= 0.35 && avgLum < 160)
        };
    },

    /**
     * Sample student ID grid (5 columns, 0-9 digits).
     */
    sampleStudentId: function (grayImg, gridConfig) {
        const w = this.CANONICAL_WIDTH;
        const h = this.CANONICAL_HEIGHT;

        let detectedDigits = [];
        let hasAmbiguity = false;

        for (let col = 0; col < gridConfig.columns; col++) {
            let maxFill = -1;
            let chosenDigit = -1;
            let markCount = 0;

            for (let row = 0; row < gridConfig.digits; row++) {
                const cx = (gridConfig.startX + col * gridConfig.colWidth) * w;
                const cy = (gridConfig.startY + row * gridConfig.rowHeight) * h;
                const r = gridConfig.radius * w;

                const sample = this.measureBubbleFill(grayImg, cx, cy, r);
                if (sample.isMarked) {
                    markCount++;
                    if (sample.fillRatio > maxFill) {
                        maxFill = sample.fillRatio;
                        chosenDigit = row;
                    }
                }
            }

            if (chosenDigit !== -1) {
                detectedDigits.push(chosenDigit.toString());
            } else {
                detectedDigits.push("?");
                hasAmbiguity = true;
            }

            if (markCount > 1) {
                hasAmbiguity = true;
            }
        }

        const idStr = detectedDigits.join("");
        return {
            studentId: idStr,
            hasAmbiguity: hasAmbiguity,
            confidence: hasAmbiguity ? 0.70 : 0.98
        };
    },

    /**
     * Sample exam version (Version A, B, C, D).
     */
    sampleVersion: function (grayImg, verConfig) {
        const w = this.CANONICAL_WIDTH;
        const h = this.CANONICAL_HEIGHT;

        let bestVersion = "A";
        let maxFill = -1;

        for (let i = 0; i < verConfig.options.length; i++) {
            const cx = (verConfig.startX + i * verConfig.spacingX) * w;
            const cy = verConfig.startY * h;
            const r = verConfig.radius * w;

            const sample = this.measureBubbleFill(grayImg, cx, cy, r);
            if (sample.isMarked && sample.fillRatio > maxFill) {
                maxFill = sample.fillRatio;
                bestVersion = verConfig.options[i];
            }
        }

        return { version: bestVersion };
    },

    /**
     * Sample question answers (1..N, A..D/E).
     */
    sampleQuestions: function (grayImg, qConfig) {
        const w = this.CANONICAL_WIDTH;
        const h = this.CANONICAL_HEIGHT;

        const optionsLetters = ['A', 'B', 'C', 'D', 'E'];
        const answersMap = {};
        const details = [];
        let hasMultiMarks = false;

        for (let q = 1; q <= qConfig.total; q++) {
            const zeroIdx = q - 1;
            const colIdx = Math.floor(zeroIdx / qConfig.rowsPerColumn);
            const rowIdx = zeroIdx % qConfig.rowsPerColumn;

            const qBaseX = (qConfig.startX + colIdx * qConfig.columnWidth) * w;
            const qBaseY = (qConfig.startY + rowIdx * qConfig.rowHeight) * h;
            const radius = qConfig.bubbleRadius * w;

            // Offset for question number label
            const bubbleStartX = qBaseX + 0.055 * w;

            const markedForQ = [];
            const optionSamples = [];

            for (let opt = 0; opt < qConfig.optionsCount; opt++) {
                const cx = bubbleStartX + opt * (qConfig.bubbleSpacingX * w);
                const cy = qBaseY;

                const sample = this.measureBubbleFill(grayImg, cx, cy, radius);
                const letter = optionsLetters[opt];

                optionSamples.push({
                    letter: letter,
                    cx: cx,
                    cy: cy,
                    radius: radius,
                    fillRatio: sample.fillRatio,
                    isMarked: sample.isMarked
                });

                if (sample.isMarked) {
                    markedForQ.push(letter);
                }
            }

            if (markedForQ.length > 1) {
                hasMultiMarks = true;
            }

            answersMap[q] = markedForQ;
            details.push({
                question: q,
                options: optionSamples,
                marked: markedForQ
            });
        }

        return {
            answers: answersMap,
            details: details,
            hasMultiMarks: hasMultiMarks
        };
    },

    /**
     * Render visually stunning grading feedback overlays on the rectified canvas.
     */
    renderGradingOverlay: function (ctx, layout, answersResult, keyAnswersMap, studentIdResult, versionResult) {
        ctx.save();

        keyAnswersMap = keyAnswersMap || {};

        // Draw overlays for each question bubble
        for (let item of answersResult.details) {
            const q = item.question;
            const correctList = keyAnswersMap[q] || [];
            const studentList = item.marked;

            for (let opt of item.options) {
                const isStudentChoice = opt.isMarked;
                const isCorrectKey = correctList.includes(opt.letter);

                if (isStudentChoice && isCorrectKey) {
                    // Correct: Green vibrant circle + checkmark
                    ctx.beginPath();
                    ctx.arc(opt.cx, opt.cy, opt.radius + 3, 0, Math.PI * 2);
                    ctx.strokeStyle = '#2e7d32'; // MudBlazor Success Dark
                    ctx.lineWidth = 3.5;
                    ctx.stroke();

                    ctx.fillStyle = 'rgba(76, 175, 80, 0.4)';
                    ctx.fill();
                } else if (isStudentChoice && !isCorrectKey) {
                    // Incorrect Student Choice: Red circle + cross
                    ctx.beginPath();
                    ctx.arc(opt.cx, opt.cy, opt.radius + 3, 0, Math.PI * 2);
                    ctx.strokeStyle = '#d32f2f'; // MudBlazor Error Dark
                    ctx.lineWidth = 3.5;
                    ctx.stroke();

                    ctx.fillStyle = 'rgba(244, 67, 54, 0.45)';
                    ctx.fill();
                } else if (!isStudentChoice && isCorrectKey && studentList.length > 0) {
                    // Missed Key Bubble: Dotted/Solid Amber highlight
                    ctx.beginPath();
                    ctx.arc(opt.cx, opt.cy, opt.radius + 4, 0, Math.PI * 2);
                    ctx.strokeStyle = '#f57c00'; // MudBlazor Warning Dark
                    ctx.lineWidth = 3;
                    ctx.setLineDash([4, 3]);
                    ctx.stroke();
                    ctx.setLineDash([]);
                }
            }
        }

        // Add Header Watermark Tag
        ctx.fillStyle = '#1e88e5';
        ctx.font = 'bold 24px Roboto, sans-serif';
        ctx.fillText(`ID: ${studentIdResult.studentId}  |  Version: ${versionResult.version}`, 50, 45);

        ctx.restore();
    }
};
