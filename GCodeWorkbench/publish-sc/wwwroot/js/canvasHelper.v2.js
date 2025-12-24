window.canvasHelper = {
    instances: {},

    init: function (elementId, dotNetRef, initialViewBox) {
        var element = document.getElementById(elementId);
        if (!element) {
            console.warn('canvasHelper: element not found:', elementId);
            return;
        }

        var svg = document.getElementById(elementId + '-svg');
        if (!svg) {
            console.warn('canvasHelper: svg not found:', elementId + '-svg');
            debug('debug-js-status', 'Error: SVG not found');
            return;
        }

        // 解析初始 viewBox
        var vb = (initialViewBox || '0 0 100 100').split(/[\s,]+/).map(Number);
        if (vb.length < 4 || vb.some(isNaN)) {
            vb = [0, 0, 100, 100];
        }

        var state = {
            vbX: vb[0],
            vbY: vb[1],
            vbW: vb[2],
            vbH: vb[3],
            initialVbX: vb[0],
            initialVbY: vb[1],
            initialVbW: vb[2],
            initialVbH: vb[3],
            panning: false,
            panStartX: 0,
            panStartY: 0,
            panStartVbX: 0,
            panStartVbY: 0,
            // Tool position (logical coords)
            toolX: 0,
            toolY: 0,
            toolVisible: false
        };

        // Debug helper
        function debug(id, msg) {
            var el = document.getElementById(id);
            if (el) el.textContent = msg;
            console.log('[CanvasHelper]', id, msg);
        }

        debug('debug-js-status', 'JS: Initialized ' + new Date().toLocaleTimeString());

        function updateViewBox() {
            var vbString = state.vbX + ' ' + state.vbY + ' ' + state.vbW + ' ' + state.vbH;
            svg.setAttribute('viewBox', vbString);
            debug('debug-viewbox', 'VB: ' + state.vbX.toFixed(1) + ' ' + state.vbY.toFixed(1) + ' ' + state.vbW.toFixed(1));
            // Update tool position on view change
            updateToolScreenPosition();
        }

        function updateToolScreenPosition() {
            var toolEl = element.querySelector('.tool-marker');
            if (!toolEl || !state.toolVisible) {
                if (toolEl) toolEl.style.display = 'none';
                return;
            }

            var rect = svg.getBoundingClientRect();
            var scaleX = rect.width / state.vbW;
            var scaleY = rect.height / state.vbH;
            var scale = Math.min(scaleX, scaleY);

            var visibleW = state.vbW * scale;
            var visibleH = state.vbH * scale;
            var offsetX = (rect.width - visibleW) / 2;
            var offsetY = (rect.height - visibleH) / 2;

            var screenX = offsetX + (state.toolX - state.vbX) * scale;
            var screenY = offsetY + (-state.toolY - state.vbY) * scale;

            toolEl.style.display = 'block';
            toolEl.style.left = screenX + 'px';
            toolEl.style.top = screenY + 'px';
        }

        function getMousePos(e) {
            var rect = svg.getBoundingClientRect();
            return {
                x: e.clientX - rect.left,
                y: e.clientY - rect.top,
                width: rect.width,
                height: rect.height
            };
        }

        // 阻止右键菜单
        element.addEventListener('contextmenu', function (e) {
            e.preventDefault();
        });

        // Helper: Calculate visible area and scale based on preserveAspectRatio="xMidYMid meet"
        function getViewTransform() {
            var rect = svg.getBoundingClientRect();
            var scaleX = rect.width / state.vbW;
            var scaleY = rect.height / state.vbH;

            // "meet" uses the smaller scale to fit everything
            var scale = Math.min(scaleX, scaleY);

            // Calculate margins
            var visibleW = state.vbW * scale;
            var visibleH = state.vbH * scale;
            var offsetX = (rect.width - visibleW) / 2;
            var offsetY = (rect.height - visibleH) / 2;

            return {
                scale: scale,
                offsetX: offsetX,
                offsetY: offsetY,
                rect: rect
            };
        }

        element.addEventListener('mousedown', function (e) {
            if (e.button === 1 || e.button === 2) {
                e.preventDefault();
                state.panning = true;
                state.panStartX = e.clientX;
                state.panStartY = e.clientY;
                state.panStartVbX = state.vbX;
                state.panStartVbY = state.vbY;
                element.style.cursor = 'grabbing';
            }
        });

        window.addEventListener('mouseup', function () {
            if (state.panning) {
                state.panning = false;
                element.style.cursor = 'crosshair';
            }
        });

        window.addEventListener('mousemove', function (e) {
            if (!state.panning) return;

            var t = getViewTransform();

            // Delta in pixels
            var pxDx = e.clientX - state.panStartX;
            var pxDy = e.clientY - state.panStartY;

            // Convert to ViewBox units using the calculated scale
            // Move ViewBox opposite to mouse to pan
            state.vbX = state.panStartVbX - (pxDx / t.scale);
            state.vbY = state.panStartVbY - (pxDy / t.scale);

            updateViewBox();
        });

        element.addEventListener('wheel', function (e) {
            e.preventDefault();

            var t = getViewTransform();

            // Mouse position relative to the SVG element
            var mx = e.clientX - t.rect.left;
            var my = e.clientY - t.rect.top;

            // Calculate mouse position relative to the ViewBox (0..1)
            // considering the effective visible area (offsets)
            var relX = (mx - t.offsetX) / (state.vbW * t.scale);
            var relY = (my - t.offsetY) / (state.vbH * t.scale);

            // Current mouse position in logical ViewBox coordinates
            var mouseVbX = state.vbX + relX * state.vbW;
            var mouseVbY = state.vbY + relY * state.vbH;

            // Zoom factor
            var factor = e.deltaY > 0 ? 1.1 : (1 / 1.1);

            // New Dimensions
            var newW = state.vbW * factor;
            var newH = state.vbH * factor;

            // Clamp
            if (newW < 0.1 || newW > 100000) return;

            // New Origin: maintain mouseVbX at the same relative position
            // Screen Pos = Origin + Rel * Dim
            // Origin = Screen Pos - Rel * Dim
            // We want mouseVbX to be at the same Screen Point.
            // Screen Point corresponds to relX in the NEW box? 
            // Yes, we want the point under cursor (relX) to remain under cursor.
            state.vbX = mouseVbX - relX * newW;
            state.vbY = mouseVbY - relY * newH;
            state.vbW = newW;
            state.vbH = newH;

            updateViewBox();
        }, { passive: false });

        // 暴露方法
        element.zoomBy = function (factor) {
            var cx = state.vbX + state.vbW / 2;
            var cy = state.vbY + state.vbH / 2;

            var newW = state.vbW * factor;
            var newH = state.vbH * factor;
            if (newW < 0.1 || newW > 100000) return;

            state.vbX = cx - newW / 2;
            state.vbY = cy - newH / 2;
            state.vbW = newW;
            state.vbH = newH;

            updateViewBox();
        };

        element.resetView = function () {
            state.vbX = state.initialVbX;
            state.vbY = state.initialVbY;
            state.vbW = state.initialVbW;
            state.vbH = state.initialVbH;
            updateViewBox();
        };

        element.setViewBox = function (x, y, w, h) {
            state.vbX = x;
            state.vbY = y;
            state.vbW = w;
            state.vbH = h;
            state.initialVbX = x;
            state.initialVbY = y;
            state.initialVbW = w;
            state.initialVbH = h;
            updateViewBox();
        };

        element.setToolPosition = function (x, y, visible) {
            state.toolX = x;
            state.toolY = y;
            state.toolVisible = visible;
            updateToolScreenPosition();
        };

        this.instances[elementId] = { element, svg, state, updateViewBox, updateToolScreenPosition };
    },

    setViewBox: function (elementId, x, y, w, h) {
        var element = document.getElementById(elementId);
        if (element && element.setViewBox) element.setViewBox(x, y, w, h);
    },

    reset: function (elementId) {
        var element = document.getElementById(elementId);
        if (element && element.resetView) element.resetView();
    },

    zoom: function (elementId, factor) {
        var element = document.getElementById(elementId);
        if (element && element.zoomBy) element.zoomBy(factor);
    },

    getSvgSize: function (elementId) {
        var element = document.getElementById(elementId);
        if (!element) return { width: 800, height: 500 };
        var svg = document.getElementById(elementId + '-svg');
        if (!svg) return { width: 800, height: 500 };
        var rect = svg.getBoundingClientRect();
        return { width: rect.width, height: rect.height };
    },

    // 将逻辑坐标 (toolX, toolY) 转换为屏幕像素坐标
    // 需要考虑 Y 轴翻转和 viewBox 变换
    getToolScreenPosition: function (elementId, toolX, toolY) {
        var instance = this.instances[elementId];
        if (!instance) return { x: 0, y: 0 };

        var svg = instance.svg;
        var state = instance.state;
        var rect = svg.getBoundingClientRect();

        // 计算缩放比例 (xMidYMid meet)
        var scaleX = rect.width / state.vbW;
        var scaleY = rect.height / state.vbH;
        var scale = Math.min(scaleX, scaleY);

        // 计算可见区域偏移 (居中)
        var visibleW = state.vbW * scale;
        var visibleH = state.vbH * scale;
        var offsetX = (rect.width - visibleW) / 2;
        var offsetY = (rect.height - visibleH) / 2;

        // 逻辑坐标转屏幕坐标
        // 注意: SVG 中有 scale(1, -1) 翻转 Y 轴
        // viewBox 的 Y 是翻转后的，所以 toolY 需要取负
        var screenX = offsetX + (toolX - state.vbX) * scale;
        var screenY = offsetY + (-toolY - state.vbY) * scale;

        return { x: screenX, y: screenY };
    },

    // 设置刀头位置（由 JS 管理屏幕坐标更新）
    setToolPosition: function (elementId, x, y, visible) {
        var element = document.getElementById(elementId);
        if (element && element.setToolPosition) element.setToolPosition(x, y, visible);
    },

    // 滚动到指定元素（用于列表自动跳转）
    scrollToElement: function (containerSelector, elementSelector) {
        try {
            var container = document.querySelector(containerSelector);
            var element = document.querySelector(elementSelector);
            if (container && element) {
                var containerRect = container.getBoundingClientRect();
                var elementRect = element.getBoundingClientRect();

                // 检查元素是否在可视区域内
                if (elementRect.top < containerRect.top || elementRect.bottom > containerRect.bottom) {
                    element.scrollIntoView({ behavior: 'smooth', block: 'center' });
                }
            }
        } catch (e) {
            console.warn('scrollToElement error:', e);
        }
    }
};
