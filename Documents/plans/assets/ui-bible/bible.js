/*
 * SS3D UI bible: builders and demo behaviour.
 * This stands in for the C# views (slot templates, panel anchoring, radial pointing). Nothing here ports as code;
 * the structure it builds is what the UXML should match, and the "uxml" button prints that structure.
 */
(() => {
    'use strict';

    // ── Asset paths ────────────────────────────────────────────────────────
    const REPO = '../../../../';
    const DIRS = {
        Inventory: 'Assets/Art/Icons/Inventory/',
        Rendered: 'Assets/Art/Icons/Rendered/',
        Interactions: 'Assets/Art/Graphics/UI/Interactions/InteractionIcons/',
        Heroicons: 'Assets/Art/Icons/Heroicons/',
        Chrome: 'Assets/Art/Graphics/UI/Chrome/',
        Harm: 'Assets/Art/Graphics/UI/Harm/',
        UI: 'Assets/Art/Graphics/UI/',
        MapEditor: 'Assets/Art/Icons/map-editor/',
        External: 'Assets/Art/Icons/External/',
    };
    // A key names a PNG unless it gives its own extension (the map editor's icons are SVGs).
    const assetPath = key => {
        const i = key.indexOf('/');
        const rest = key.slice(i + 1);
        return DIRS[key.slice(0, i)] + rest + (/\.\w+$/.test(rest) ? '' : '.png');
    };
    const cssUrl = key => `url("${REPO}${encodeURI(assetPath(key))}")`;

    // ── Tiny element helper ────────────────────────────────────────────────
    function h(tag, cls, opts) {
        const e = document.createElement(tag);
        if (cls) e.className = cls;
        if (opts) {
            if (opts.text != null) e.textContent = opts.text;
            if (opts.icon) setIcon(e, opts.icon);
            if (opts.style) Object.assign(e.style, opts.style);
        }
        return e;
    }

    function setIcon(el, key) {
        el.classList.add('sprite');
        el.style.backgroundImage = cssUrl(key);
        el.dataset.iconKey = key;
    }

    const $ = (sel, root = document) => root.querySelector(sel);
    const $$ = (sel, root = document) => Array.from(root.querySelectorAll(sel));

    const store = {
        get(k) { try { return JSON.parse(localStorage.getItem('ss3d-ui-bible:' + k)); } catch { return null; } },
        set(k, v) { try { localStorage.setItem('ss3d-ui-bible:' + k, JSON.stringify(v)); } catch { /* private mode */ } },
    };

    const toastEl = $('#toast');
    let toastTimer = 0;
    function toast(msg) {
        toastEl.textContent = msg;
        toastEl.classList.add('is-shown');
        clearTimeout(toastTimer);
        toastTimer = setTimeout(() => toastEl.classList.remove('is-shown'), 1600);
    }

    async function copy(text, label) {
        try {
            await navigator.clipboard.writeText(text);
            toast(label || 'copied');
        } catch {
            window.prompt('Copy:', text);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // Inventory slot
    // data-slot="Rendered/Radio | active | L | glyph | frame | sm | wide | frameless"
    // The first token is the sprite: Rendered/* draws as an item, anything else as a placeholder unless `glyph`
    // says it stands in for an item render.
    // ══════════════════════════════════════════════════════════════════════
    const SLOT_MODS = new Set(['active', 'frame', 'frameless', 'sm', 'wide']);

    function parseSlot(str) {
        const o = { mods: [] };
        str.split('|').map(s => s.trim()).filter(Boolean).forEach((t, i) => {
            if (i === 0 && t.includes('/')) {
                o.icon = t;
                o.kind = t.startsWith('Rendered/') ? 'item' : 'placeholder';
                return;
            }
            if (SLOT_MODS.has(t)) o.mods.push(t);
            else if (t === 'glyph' || t === 'item' || t === 'placeholder') o.kind = t;
            else if (t === 'L') o.hand = 'left';
            else if (t === 'R') o.hand = 'right';
        });
        return o;
    }

    function buildSlot(o) {
        const root = h('div', 'inventory-slot');
        o.mods.forEach(m => root.classList.add('inventory-slot--' + m));
        const well = h('div', 'inventory-slot__well');
        root.append(well);
        if (o.icon) {
            const kindCls = o.kind === 'glyph' ? ' inventory-slot__icon--glyph'
                : o.kind === 'placeholder' ? ' inventory-slot__icon--placeholder' : '';
            well.append(h('div', 'inventory-slot__icon' + kindCls, { icon: o.icon }));
        }
        if (o.hand) well.append(h('div', `inventory-slot__hand-mark inventory-slot__hand-mark--${o.hand}`, { text: o.hand === 'left' ? 'L' : 'R' }));
        return root;
    }

    function expandSlots(scope = document) {
        $$('[data-slot]', scope).forEach(el => {
            const s = buildSlot(parseSlot(el.dataset.slot));
            el.classList.forEach(c => s.classList.add(c));
            if (el.id) s.id = el.id;
            el.replaceWith(s);
        });
    }

    // ══════════════════════════════════════════════════════════════════════
    // Storage panel
    // data-items="Rendered/Pen; Rendered/ToolboxNote id=inner; ; …"   (id= names a cell other panels anchor to)
    // ══════════════════════════════════════════════════════════════════════
    function buildCell(spec) {
        const t = spec.trim().split(/\s+/).filter(Boolean);
        const c = h('div', 'storage-slot');
        t.forEach(f => {
            if (f.startsWith('id=')) c.id = f.slice(3);
            else if (f.includes('/')) c.append(h('div', 'storage-slot__icon', { icon: f }));
        });
        return c;
    }

    function buildStorage(el) {
        const cols = Number(el.dataset.cols || 4);
        const items = (el.dataset.items || '').split(';');
        const p = h('div', 'storage-panel' + (el.dataset.anchor ? ' anchored' : ''));
        p.dataset.panel = el.dataset.storage;
        if (el.dataset.anchor) p.dataset.anchor = el.dataset.anchor;

        const grid = h('div', 'storage-panel__grid');
        const rows = Math.ceil(items.length / cols);
        for (let r = 0; r < rows; r++) {
            const row = h('div', 'storage-panel__row');
            for (let c = 0; c < cols; c++) row.append(buildCell(items[r * cols + c] || ''));
            grid.append(row);
        }
        p.append(grid);
        const caret = h('div', 'storage-panel__caret');
        if (!el.dataset.anchor) caret.style.left = '50%'; // a free-standing panel points from its middle
        p.append(caret);
        el.replaceWith(p);
    }

    // ── Anchoring: place a panel above its source, caret on the source's centre ──
    // Measures through a 100 px probe so it holds under CSS zoom and transform: scale alike.
    function probe(root) {
        let p = root.querySelector(':scope > .k-probe');
        if (!p) {
            p = document.createElement('div');
            p.className = 'k-probe';
            p.style.cssText = 'position:absolute;left:0;top:0;width:100px;height:1px;visibility:hidden;pointer-events:none';
            root.prepend(p);
        }
        return p;
    }

    function local(el, root) {
        const pr = probe(root).getBoundingClientRect();
        const k = pr.width / 100;
        const r = el.getBoundingClientRect();
        return { x: (r.left - pr.left) / k, y: (r.top - pr.top) / k, w: r.width / k, h: r.height / k, k };
    }

    // Two passes: a panel anchored inside another panel needs its parent placed first, whatever the DOM order.
    function anchorAll(root) {
        anchorPass(root);
        anchorPass(root);
    }

    function anchorPass(root) {
        $$('.storage-panel[data-anchor]', root).forEach(panel => {
            const src = $(panel.dataset.anchor, root);
            if (!src || panel.offsetParent === null || src.offsetParent === null) return;
            const s = local(src, root);
            const pw = local(panel, root);
            const rootW = root.getBoundingClientRect().width / s.k;
            // A source inside another panel: clear that whole panel, keep the caret on the cell.
            const host = src.closest('.storage-panel');
            const top = host ? local(host, root).y : s.y;
            let left = s.x + s.w / 2 - pw.w / 2;
            left = Math.max(6, Math.min(left, rootW - pw.w - 6));
            panel.style.left = left + 'px';
            panel.style.top = (top - pw.h - 18) + 'px';
            panel.querySelector('.storage-panel__caret').style.left = (s.x + s.w / 2 - left) + 'px';
        });
    }

    // An element in a hidden tab has no boxes, so nothing in it can be measured or placed yet.
    const shown = el => el.getClientRects().length > 0;
    const anchorRoots = () => $$('.anchor-root');
    const reanchor = () => {
        $$('[data-cmp]').forEach(fitSide);
        anchorRoots().forEach(anchorAll);
        $$('[data-cmp]').forEach(placeRef);
        const parts = $('#parts-table');
        if (parts && shown(parts)) refreshParts(parts);
    };

    // Clicking a container toggles its panel (a stand-in for the radial's Open); closing a panel closes the chain above it.
    function wireContainers(root) {
        root.addEventListener('click', e => {
            const src = e.target.closest('.inventory-slot, .storage-slot');
            const panel = src && $$('.storage-panel[data-anchor]', root).find(p => $(p.dataset.anchor, root) === src);
            if (panel) setPanel(root, panel, panel.classList.contains('is-hidden'));
        });
    }

    function setPanel(root, panel, open) {
        panel.classList.toggle('is-hidden', !open);
        if (!open) {
            // Close anything opened from inside this panel.
            $$('.storage-panel[data-anchor]', root).forEach(child => {
                const s = $(child.dataset.anchor, root);
                if (s && panel.contains(s) && !child.classList.contains('is-hidden')) setPanel(root, child, false);
            });
        }
        anchorAll(root);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Equipment grid, paperdoll, pile picker, radial
    // ══════════════════════════════════════════════════════════════════════
    // Both references draw every equipment glyph at the same dim alpha, and the pocket with no frame.
    const EQUIPMENT = [
        ['Inventory/BeepBack', 'Inventory/BeepHead', 'Inventory/BeepPocket|frameless'],
        ['Inventory/Eyes', 'Inventory/Face', 'Inventory/Ears'],
        ['Inventory/Shirt', 'Inventory/Suit', 'Inventory/Hands'],
        ['UI/Card', 'Inventory/Feet', 'Inventory/Waist'],
    ];

    // A slot spec without its sprite: what `data-empty` builders draw. A frameless slot (the pocket) is nothing but its
    // glyph, so it keeps it.
    const bare = spec => spec.includes('frameless') ? spec : spec.split('|').filter(t => !t.includes('/')).join('|');

    function buildEquipment(el) {
        const empty = 'empty' in el.dataset;
        const g = h('div', 'equipment-grid');
        EQUIPMENT.forEach(row => {
            const r = h('div', 'equipment-grid__row');
            row.forEach(s => r.append(buildSlot(parseSlot((empty ? bare(s) : s) + '|frame'))));
            g.append(r);
        });
        el.replaceWith(g);
    }

    // Slots by body part, seen from the front: their right hand (R) on your left. Positions live in components.css
    // (.character-paperdoll__slot--<part>).
    const PAPERDOLL = [
        ['eyes', 'Inventory/Eyes'],
        ['head', 'Inventory/BeepHead'],
        ['mask', 'Inventory/Face'],
        ['ears', 'Inventory/Ears'],
        ['uniform', 'Inventory/Shirt'],
        ['suit', 'Inventory/Suit'],
        ['pocket', 'Inventory/BeepPocket|frameless'],
        ['hand-right', 'Rendered/Pen|R'],
        ['belt', 'Inventory/Waist'],
        ['hand-left', 'Inventory/HandLeft|L'],
        ['gloves', 'Inventory/Hands'],
        ['id', 'UI/Card'],
        ['back', 'Inventory/BeepBack'],
        ['shoes', 'Inventory/Feet'],
    ];

    function buildPaperdoll(el) {
        const empty = 'empty' in el.dataset;
        const sheet = h('div', 'character-paperdoll');
        if (!empty) sheet.append(h('div', 'character-paperdoll__silhouette sprite'));
        PAPERDOLL.forEach(([part, spec]) => {
            const slot = buildSlot(parseSlot((empty ? bare(spec) : spec) + '|frame|sm'));
            slot.classList.add('character-paperdoll__slot--' + part);
            sheet.append(slot);
        });
        el.replaceWith(sheet);
    }

    // The reference pile is nine soda cans. There is no can render, so the CD stands in for it here and in storage.
    const CAN = 'Rendered/CD';

    function buildPile(el) {
        const empty = 'empty' in el.dataset;
        const p = h('div', 'pile-picker');
        for (let r = 0; r < 3; r++) {
            const row = h('div', 'pile-picker__row');
            for (let c = 0; c < 3; c++) {
                const cell = h('div', 'pile-picker__cell');
                if (!empty) cell.append(h('div', 'pile-picker__shadow', { icon: CAN }), h('div', 'pile-picker__icon', { icon: CAN }));
                row.append(cell);
            }
            p.append(row);
        }
        el.replaceWith(p);
    }

    // The reference's verbs. dir: 0 = up, then clockwise in 60° steps (six directions).
    const RADIAL_STEP = 60;
    const RADIAL = [
        // size: the icon box that makes each sprite's drawing look ~24 px (they fill 0.79-0.86 of their box).
        { dir: 0, icon: 'Interactions/Open', label: 'Open', size: 28 },
        { dir: 1, icon: 'Interactions/Power', label: 'Turn on', size: 28 },
        { dir: 2, icon: 'Heroicons/Outline/Logout', label: 'Enter', mirrored: true, size: 30 },
    ];

    // The wedge: a band `spread` degrees wide from the hub's edge (hubR) to the rim,
    // pointing up. The same two arcs fill it with Painter2D in Unity (see components.css).
    const SVG_NS = 'http://www.w3.org/2000/svg';

    function radialWedge(size, hubR, spread = RADIAL_STEP) {
        const c = size / 2;
        const half = spread / 2 * Math.PI / 180;
        const pt = (r, side) => `${(c + r * Math.sin(side * half)).toFixed(2)} ${(c - r * Math.cos(side * half)).toFixed(2)}`;
        const svg = document.createElementNS(SVG_NS, 'svg');
        svg.setAttribute('class', 'radial-interaction-menu__wedge');
        svg.setAttribute('viewBox', `0 0 ${size} ${size}`);
        const path = document.createElementNS(SVG_NS, 'path');
        path.setAttribute('d', `M ${pt(hubR, -1)} L ${pt(c, -1)} A ${c} ${c} 0 0 1 ${pt(c, 1)} L ${pt(hubR, 1)} A ${hubR} ${hubR} 0 0 0 ${pt(hubR, -1)} Z`);
        svg.append(path);
        return svg;
    }

    function buildRadial(el) {
        const m = h('div', 'radial-interaction-menu');
        m.append(h('div', 'radial-interaction-menu__disc'));
        const cs = getComputedStyle(document.documentElement);
        // The wedge starts 1 px under the hub's edge, like the disc's band, so no seam shows between them.
        const wedge = radialWedge(parseFloat(cs.getPropertyValue('--ui-radial-size')), parseFloat(cs.getPropertyValue('--ui-radial-hub-size')) / 2 - 1);
        m.append(wedge);
        const hub = h('div', 'radial-interaction-menu__hub');
        hub.append(h('div', 'radial-interaction-menu__hub-plate'));
        const empty = 'empty' in el.dataset;
        if (!empty) hub.append(h('div', 'radial-interaction-menu__hub-item', { icon: 'Rendered/Radio' }));
        m.append(hub);
        if (!empty) RADIAL.forEach(o => {
            const a = o.dir * RADIAL_STEP * Math.PI / 180;
            const p = h('div', 'radial-interaction-petal',
                { style: { left: (50 + Math.sin(a) * 34.5) + '%', top: (50 - Math.cos(a) * 34.5) + '%' } });
            const icon = h('div', 'radial-interaction-petal__icon' + (o.mirrored ? ' radial-interaction-petal__icon--mirrored' : ''), { icon: o.icon });
            // Keep a 32 px slot whatever the icon's size, so labels line up.
            if (o.size) Object.assign(icon.style, { width: o.size + 'px', height: o.size + 'px', margin: (32 - o.size) / 2 + 'px' });
            p.append(icon);
            p.append(h('div', 'radial-interaction-petal__label', { text: o.label }));
            m.append(p);
        });

        let cur = 0;
        const point = deg => {
            const d = ((deg - cur) % 360 + 540) % 360 - 180;
            cur += d;
            wedge.style.rotate = cur + 'deg';
        };
        const enabled = RADIAL.map(o => o.dir * RADIAL_STEP);
        m.addEventListener('pointermove', e => {
            const r = m.getBoundingClientRect();
            const dx = e.clientX - (r.left + r.width / 2);
            const dy = e.clientY - (r.top + r.height / 2);
            if (Math.hypot(dx, dy) < r.width * 0.19) return; // over the hub
            const ang = (Math.atan2(dx, -dy) * 180 / Math.PI + 360) % 360;
            const best = enabled.reduce((b, d) => {
                const diff = Math.abs(((ang - d) % 360 + 540) % 360 - 180);
                return diff < b.diff ? { d, diff } : b;
            }, { d: enabled[0], diff: 999 });
            if (best.diff <= 30) point(best.d);
        });
        const start = RADIAL.find(o => o.label === el.dataset.point);
        if (start) point(start.dir * RADIAL_STEP);
        el.replaceWith(m);
    }

    // ══════════════════════════════════════════════════════════════════════
    // UXML printer: the element tree as UXML, for the port
    // ══════════════════════════════════════════════════════════════════════
    function toUxml(el, d = 0) {
        const pad = '    '.repeat(d);
        const cls = Array.from(el.classList).filter(c => c !== 'sprite' && c !== 'anchored' && c !== 'is-hidden' && !c.startsWith('demo') && !c.startsWith('spec__')).join(' ');
        const kids = el.namespaceURI === SVG_NS ? []
            : Array.from(el.children).filter(k => !k.classList.contains('k-probe') && !k.classList.contains('cap'));
        const text = kids.length === 0 ? el.textContent.trim() : '';
        const tag = text ? 'ui:Label' : 'ui:VisualElement';
        let attrs = cls ? ` class="${cls}"` : '';
        if (text) attrs += ` text="${text.replace(/"/g, '&quot;')}"`;
        const style = [];
        if (el.dataset.iconKey) style.push(`background-image: url('project://database/${assetPath(el.dataset.iconKey)}')`);
        if (el.style.width && !el.dataset.iconKey) style.push(`width: ${el.style.width}`);
        if (el.style.rotate) style.push(`rotate: ${el.style.rotate}`);
        if (el.style.left && el.classList.contains('storage-panel__caret')) style.push(`left: ${el.style.left.endsWith('%') ? el.style.left : Math.round(parseFloat(el.style.left)) + 'px'}`);
        if (style.length) attrs += ` style="${style.join('; ')};"`;
        if (!kids.length) return `${pad}<${tag}${attrs} />`;
        return `${pad}<${tag}${attrs}>\n${kids.map(k => toUxml(k, d + 1)).join('\n')}\n${pad}</${tag}>`;
    }

    // ══════════════════════════════════════════════════════════════════════
    // Specimen chrome: backdrop switcher + uxml copy
    // ══════════════════════════════════════════════════════════════════════
    const BACKDROPS = ['floor', 'floor-green', 'dark', 'menu'];

    function wireSpecs() {
        $$('.spec').forEach(spec => {
            const tools = $('.spec__tools', spec);
            const stage = $('.spec__stage', spec);
            if (!tools || !stage) return;
            const current = BACKDROPS.find(b => stage.classList.contains('bd-' + b));
            BACKDROPS.forEach(b => {
                const btn = h('button', 'chip-btn' + (b === current ? ' is-on' : ''), { text: b });
                btn.type = 'button';
                btn.addEventListener('click', () => {
                    BACKDROPS.forEach(x => stage.classList.remove('bd-' + x));
                    stage.classList.add('bd-' + b);
                    $$('.chip-btn', tools).forEach(x => x.classList.toggle('is-on', x === btn));
                });
                tools.append(btn);
            });
            const ux = h('button', 'chip-btn', { text: 'uxml' });
            ux.type = 'button';
            ux.title = 'Copy this specimen as UXML';
            ux.addEventListener('click', () => {
                const zoom = $('.spec__zoom', spec);
                const body = Array.from(zoom.children).filter(c => !c.classList.contains('k-probe')).map(c => toUxml(c, 1)).join('\n');
                copy(`<ui:UXML xmlns:ui="UnityEngine.UIElements">\n${body}\n</ui:UXML>\n`, 'UXML copied');
            });
            tools.append(ux);
        });
    }

    // ══════════════════════════════════════════════════════════════════════
    // Stages: 1200 × 675 canvases scaled to their column (or the screen)
    // ══════════════════════════════════════════════════════════════════════
    // Side by side puts the reference layer (same size, same scale) to the right of the canvas.
    function fitStage(stage) {
        const vp = $('.stage__viewport', stage);
        const cv = $('.stage__canvas', stage);
        const ref = $('.stage__ref', stage);
        const mode = stage.dataset.cmpMode;
        const side = mode === 'side';
        const wide = 1200 * (side ? 2 : 1);
        const tall = 675 + (mode && mode !== 'ui' && stage._cmp ? stage._cmp.extra || 0 : 0);
        const fs = document.fullscreenElement === vp;
        const w = vp.clientWidth;
        let s;
        let x = 0;
        let y = 0;
        if (fs) {
            const hh = vp.clientHeight;
            s = Math.min(w / wide, hh / tall);
            x = (w - wide * s) / 2;
            y = (hh - tall * s) / 2;
        } else {
            s = w / wide;
            vp.style.height = tall * s + 'px';
        }
        const put = (el, left) => Object.assign(el.style, { transform: `scale(${s})`, left: left + 'px', top: y + 'px' });
        put(cv, x);
        if (ref) {
            put(ref, x + (side ? 1200 * s : 0));
            ref.style.height = tall + 'px';
        }
    }

    function wireStages() {
        $$('.stage').forEach(stage => {
            const vp = $('.stage__viewport', stage);
            const cv = $('.stage__canvas', stage);
            // fitStage sets the viewport's height, which this also watches: only a new width needs a refit.
            let lastW = -1;
            new ResizeObserver(() => {
                if (vp.clientWidth === lastW) return;
                lastW = vp.clientWidth;
                fitStage(stage);
                placeRef(stage);
            }).observe(vp);
            const fsBtn = $('[data-fullscreen]', stage);
            if (fsBtn) fsBtn.addEventListener('click', () => {
                if (document.fullscreenElement) document.exitFullscreen();
                else vp.requestFullscreen().catch(() => toast('full screen is blocked here'));
            });
        });
        document.addEventListener('fullscreenchange', () => $$('.stage').forEach(fitStage));
    }

    // ══════════════════════════════════════════════════════════════════════
    // Controls: hands, tabs, segmented, dropdowns, sliders
    // ══════════════════════════════════════════════════════════════════════
    function wireHands() {
        document.addEventListener('click', e => {
            const s = e.target.closest('[data-hands] > .inventory-slot');
            if (!s) return;
            $$(':scope > .inventory-slot', s.parentElement).forEach(x => x.classList.toggle('inventory-slot--active', x === s));
        });
    }

    function wireMenus() {
        document.addEventListener('click', e => {
            // The states tab shows fixed states: its controls don't change on click.
            if (e.target.closest('[data-states]')) return;
            const tab = e.target.closest('[data-tabs] > .menu-tab');
            if (tab && !tab.classList.contains('menu-tab--disabled')) {
                const tabs = tab.parentElement;
                $$(':scope > .menu-tab', tabs).forEach(t => t.classList.toggle('menu-tab--selected', t === tab));
                const screen = tabs.closest('.cs');
                if (screen && tab.dataset.tab) {
                    $$('.cs-pane', screen).forEach(p => p.classList.toggle('is-hidden', p.dataset.pane !== tab.dataset.tab));
                    // The reference follows the tab when there is a screenshot of it.
                    const host = screen.closest('[data-cmp]');
                    const i = host && host._cmp ? host._cmp.refs.findIndex(r => r.tab === tab.dataset.tab) : -1;
                    if (i >= 0) setRef(host, i);
                }
            }

            const seg = e.target.closest('.menu-segmented__option');
            if (seg) {
                $$(':scope > .menu-segmented__option', seg.parentElement).forEach(o => o.classList.toggle('menu-segmented__option--selected', o === seg));
                updateCode();
            }

            const item = e.target.closest('.menu-dropdown__item');
            const dd = e.target.closest('.menu-dropdown');
            $$('.menu-dropdown--open').forEach(o => { if (o !== dd && !o.closest('[data-states]')) o.classList.remove('menu-dropdown--open'); });
            if (item && dd) {
                $('.menu-dropdown__value', dd).textContent = item.textContent;
                dd.classList.remove('menu-dropdown--open');
            } else if (dd) {
                dd.classList.toggle('menu-dropdown--open');
            }

            const tile = e.target.closest('.style-tile');
            if (tile) $$(':scope > .style-tile', tile.parentElement).forEach(t => t.classList.toggle('style-tile--selected', t === tile));
        });
    }

    function setSlider(sl, v) {
        v = Math.max(0, Math.min(1, v));
        sl.dataset.value = v.toFixed(3);
        $('.menu-slider__fill', sl).style.width = v * 100 + '%';
        $('.menu-slider__dragger', sl).style.left = v * 100 + '%';
        if (sl.dataset.bind) onBind(sl.dataset.bind, v);
    }

    function wireSliders() {
        $$('.menu-slider').forEach(sl => {
            setSlider(sl, Number(sl.dataset.value || 0.5));
            if (sl.closest('[data-states]')) return; // the states tab shows fixed states
            const fromEvent = e => {
                const r = sl.getBoundingClientRect();
                setSlider(sl, (e.clientX - r.left) / r.width);
            };
            sl.addEventListener('pointerdown', e => {
                sl.setPointerCapture(e.pointerId);
                sl.classList.add('menu-slider--dragging');
                fromEvent(e);
            });
            sl.addEventListener('pointermove', e => { if (sl.hasPointerCapture(e.pointerId)) fromEvent(e); });
            sl.addEventListener('pointerup', () => sl.classList.remove('menu-slider--dragging'));
        });
    }

    // ── Character setup bindings ───────────────────────────────────────────
    // Hair starts where the reference's marks sit: orange hue, picked low on the dark side of the triangle.
    const cs = { skintone: 0.36, height: 0.84, fat: 0.04, breasts: 0.62, turn: 0.5, hue: 24, mix: 0.82, light: 0.14 };

    function onBind(key, v) {
        cs[key] = v;
        const root = $('#cs-canvas');
        if (!root) return;
        root.style.setProperty('--cs-skin', (1.05 - cs.skintone * 0.7).toFixed(3));
        root.style.setProperty('--cs-h', (0.86 + cs.height * 0.18).toFixed(3));
        root.style.setProperty('--cs-w', (0.92 + cs.fat * 0.3).toFixed(3));
        root.style.setProperty('--cs-turn', Math.cos((cs.turn - 0.5) * Math.PI).toFixed(3));
        updateCode();
    }

    function updateCode() {
        const code = $('#cs-code');
        if (!code) return;
        const sex = $$('#cs-canvas [data-bind="sex"] .menu-segmented__option').findIndex(o => o.classList.contains('menu-segmented__option--selected'));
        const name = ($('#cs-canvas [data-bind="name"]') || {}).value || '';
        code.textContent = JSON.stringify({ name, sex: Math.max(0, sex), race: 0, skintone: +cs.skintone.toFixed(2), height: +cs.height.toFixed(2), fat: +cs.fat.toFixed(2), hair: hairHex() });
    }

    // Hair colour: hue from the ring, then mix toward a grey picked across the triangle.
    function hsv(hue) {
        const f = n => {
            const k = (n + hue / 60) % 6;
            return 255 * (1 - Math.max(0, Math.min(k, 4 - k, 1)));
        };
        return [f(5), f(3), f(1)];
    }

    function hairRgb() {
        const base = hsv(cs.hue);
        const grey = 255 * cs.light;
        return base.map(c => Math.round(c * (1 - cs.mix) + grey * cs.mix));
    }

    const hairHex = () => '#' + hairRgb().map(c => c.toString(16).padStart(2, '0')).join('');

    function wireColorPicker() {
        const pk = $('#cs-color');
        if (!pk) return;
        const root = $('#cs-canvas');
        const mark = $('.color-picker__mark', pk);
        const hueMark = $('.color-picker__hue-mark', pk);
        const apply = () => {
            pk.style.setProperty('--picker-hue', `rgb(${hsv(cs.hue).join(',')})`);
            root.style.setProperty('--hair', hairHex());
            // The style tiles show the pure hue, as in the reference; the character wears the picked colour.
            root.style.setProperty('--hair-tile', `rgb(${hsv(cs.hue).map(Math.round).join(',')})`);
            // Hue marker on the ring (cyan at the top, clockwise).
            const a = ((cs.hue - 180) * Math.PI) / 180;
            hueMark.style.left = 50 + Math.sin(a) * 46 + '%';
            hueMark.style.top = 50 - Math.cos(a) * 46 + '%';
            // Mark inside the triangle: top = pure hue, bottom edge = black → white.
            const y = cs.mix;
            const half = y / 2;
            const x = 0.5 - half + (cs.light * y);
            mark.style.left = 19.7 + x * 62.5 + '%';
            mark.style.top = 12.3 + y * 55.2 + '%';
            updateCode();
        };
        const pick = e => {
            const r = pk.getBoundingClientRect();
            const px = (e.clientX - r.left) / r.width;
            const py = (e.clientY - r.top) / r.height;
            const dx = px - 0.5;
            const dy = py - 0.5;
            if (Math.hypot(dx, dy) > 0.41) {
                const ang = (Math.atan2(dx, -dy) * 180 / Math.PI + 360) % 360;
                cs.hue = (180 + ang) % 360;
            } else {
                const ty = Math.max(0.001, Math.min(1, (py - 0.123) / 0.552));
                const tx = (px - 0.197) / 0.625;
                cs.mix = ty;
                cs.light = Math.max(0, Math.min(1, (tx - (0.5 - ty / 2)) / ty));
            }
            apply();
        };
        pk.addEventListener('pointerdown', e => { pk.setPointerCapture(e.pointerId); pick(e); });
        pk.addEventListener('pointermove', e => { if (pk.hasPointerCapture(e.pointerId)) pick(e); });
        apply();
    }

    const HAIR_STYLES = [
        [],
        ['pony', 'pony-tail'],
        ['bob'],
        ['afro'],
    ];

    function buildStyles() {
        const grid = $('#cs-styles');
        if (!grid) return;
        HAIR_STYLES.forEach((parts, i) => {
            const t = h('div', 'style-tile' + (i === 1 ? ' style-tile--selected' : ''));
            const sil = h('div', 'sil');
            const behind = parts.filter(p => p === 'afro');
            behind.forEach(p => sil.append(h('div', 'sil-hair sil-hair--' + p)));
            sil.append(h('div', 'sil-shoulders'));
            sil.append(h('div', 'sil-head'));
            parts.filter(p => !behind.includes(p)).forEach(p => sil.append(h('div', 'sil-hair sil-front sil-hair--' + p)));
            t.append(sil);
            grid.append(t);
        });
    }

    // ══════════════════════════════════════════════════════════════════════
    // Foundations: swatches, sizes, icons
    // ══════════════════════════════════════════════════════════════════════
    const TOKEN_GROUPS = [
        ['Surfaces over the world', [['--ui-well', 'slot fill'], ['--ui-well-hover', 'slot hover'], ['--ui-well-faint', 'equipment slot fill'], ['--ui-panel', 'container panel'], ['--ui-panel-edge', 'panel border'], ['--ui-cell-edge', 'container cell outline'], ['--ui-sheet', 'search sheet'], ['--ui-plate', 'pile picker'], ['--ui-radial-hub', 'radial hub'], ['--ui-float', 'dropdown list, popover, hint']]],
        ['Lines', [['--ui-line-active', 'active hand'], ['--ui-line-frame', 'slot outline'], ['--ui-sheet-line', 'search sheet slot'], ['--ui-line-frame-hover', 'slot outline, hover']]],
        ['Text', [['--ui-text', 'primary, active hand mark'], ['--ui-mark', 'idle hand mark'], ['--ui-text-faint', 'placeholder'], ['--ui-text-on-light', 'on white plates']]],
        ['Menus', [['--ui-menu-tab', 'idle tab, segmented'], ['--ui-menu-field', 'inputs'], ['--ui-menu-selected', 'selected, primary button'], ['--ui-menu-rule', 'title rule'], ['--ui-frame-outer', 'preview frame line'], ['--ui-frame-mat', 'preview frame mat']]],
        ['Map editor severity (new)', [['--ui-danger', 'error'], ['--ui-warn', 'warning'], ['--ui-ok', 'clear']]],
    ];

    function buildSwatches() {
        const host = $('#swatches');
        TOKEN_GROUPS.forEach(([title, toks]) => {
            host.append(h('h3', '', { text: title }));
            const grid = h('div', 'swatches');
            toks.forEach(([name, use]) => {
                const sw = h('div', 'sw');
                const chip = h('div', 'sw__chip');
                chip.style.backgroundImage = `linear-gradient(var(${name}), var(${name})), linear-gradient(90deg, #c3bfb5 0 50%, #0a0a0a 50%)`;
                const meta = h('div', 'sw__meta');
                meta.append(h('div', 'sw__name', { text: name }));
                const val = h('div', 'sw__val');
                val.dataset.token = name;
                meta.append(val);
                meta.append(h('div', 'sw__use', { text: use }));
                sw.append(chip, meta);
                grid.append(sw);
            });
            host.append(grid);
        });
        refreshSwatchValues();
    }

    function refreshSwatchValues() {
        const cs = getComputedStyle(document.documentElement);
        $$('.sw__val').forEach(v => { v.textContent = cs.getPropertyValue(v.dataset.token).trim(); });
    }

    function buildSizes() {
        const host = $('#sizes-list');
        if (!host) return;
        const cs = getComputedStyle(document.documentElement);
        const px = n => parseFloat(cs.getPropertyValue(n));
        const rad = px('--ui-slot-radius');
        [
            ['slot', px('--ui-slot'), px('--ui-slot'), rad],
            ['hand, wide', px('--ui-slot-hand-wide'), px('--ui-slot'), px('--ui-frame-radius')],
            ['frame', px('--ui-slot'), px('--ui-slot'), px('--ui-frame-radius')],
            ['paperdoll', px('--ui-slot-sm'), px('--ui-slot-sm'), px('--ui-slot-sm-radius')],
            ['container cell', px('--ui-cell'), px('--ui-cell'), px('--ui-cell-radius')],
            ['pile cell', px('--ui-pile-cell'), px('--ui-pile-cell'), px('--ui-cell-radius')],
            ['tab', 141, 40, 0],
            ['radial', px('--ui-radial-size') / 3, px('--ui-radial-size') / 3, 999],
        ].forEach(([name, w, hh, r]) => {
            const s = h('div', 'size');
            s.append(h('div', 'size__box', { style: { width: w + 'px', height: hh + 'px', borderRadius: r + 'px' } }));
            const label = name === 'radial' ? `radial ${px('--ui-radial-size')} (⅓ scale)` : `${name} ${w}×${hh}`;
            s.append(h('div', '', { text: label }));
            host.append(s);
        });
        const sp = h('div', 'size');
        const bars = h('div', '', { style: { display: 'flex', gap: '6px', alignItems: 'flex-end', marginBottom: '6px', height: '40px' } });
        [1, 2, 3, 4, 6].forEach(n => bars.append(h('div', '', { style: { width: px('--ui-space-' + n) + 'px', height: px('--ui-space-' + n) + 'px', background: 'rgba(255,255,255,.35)' } })));
        sp.append(bars, h('div', '', { text: 'space 4 · 8 · 12 · 16 · 24' }));
        host.append(sp);
    }

    // The library lists only the sprites this page draws: every icon the builders set, plus the ones CSS loads.
    const CSS_SPRITES = ['Chrome/DropdownArrow', 'Harm/HarmBase'];
    const ICON_GROUP_TITLES = [
        ['Inventory', 'Inventory · Assets/Art/Icons/Inventory'],
        ['UI', 'Only in Graphics/UI'],
        ['Interactions', 'Interactions · Graphics/UI/Interactions/InteractionIcons'],
        ['Heroicons', 'Heroicons · Assets/Art/Icons/Heroicons'],
        ['Rendered', 'Rendered items · Assets/Art/Icons/Rendered'],
        ['Chrome Harm', 'Chrome sprites · Assets/Art/Graphics/UI'],
    ];

    function buildIcons() {
        const host = $('#icon-groups');
        const used = new Set($$('[data-icon-key]').map(e => e.dataset.iconKey).concat(CSS_SPRITES));
        ICON_GROUP_TITLES.forEach(([prefixes, title]) => {
            const keys = [...used].filter(k => prefixes.split(' ').includes(k.slice(0, k.indexOf('/')))).sort();
            if (!keys.length) return;
            host.append(h('h3', '', { text: title }));
            const grid = h('div', 'icon-grid');
            keys.forEach(key => {
                const t = h('div', 'icon-tile');
                const img = h('div', 'icon-tile__img');
                img.style.backgroundImage = cssUrl(key);
                t.append(img, h('div', 'icon-tile__name', { text: key.split('/').pop() }));
                t.title = assetPath(key);
                t.addEventListener('click', () => {
                    copy(assetPath(key), assetPath(key));
                    t.classList.add('is-copied');
                    setTimeout(() => t.classList.remove('is-copied'), 900);
                });
                grid.append(t);
            });
            host.append(grid);
        });
    }

    // ══════════════════════════════════════════════════════════════════════
    // Compare with the reference screenshots: side by side, or the UI laid over the reference
    // ══════════════════════════════════════════════════════════════════════
    // Each entry pins one point of its screenshot (`at`, image px) to a corner of an element in the specimen (`to`;
    // top-left, or the centre with corner 'c') at the screenshot's own scale (`scale`, image px per reference px).
    // Sizes are never fitted, so a size difference shows. Most screenshots are ~1.87 (a 64 px slot is ~119 screenshot
    // px), the character setup ones included; `scale` is set per screenshot.
    const REF_DIR = 'references/';
    // `blur` is the screenshot's own softness (Gaussian sigma, screenshot px), used by "low res". `floor` is the
    // world tone around the UI in the screenshot: side by side puts the specimen on it, so both halves share a surround.
    const CMP = {
        frame: [{ img: 'equipment-open-and-hands', scale: 1.86, blur: 2, floor: '#000000', at: [960, 391], to: '.inventory-slot--wide' }],
        strip: [
            { img: 'quick-inventory-full', scale: 1.83, blur: 2, floor: '#6b604e', at: [160, 76], to: '.inventory-slot' },
            { img: 'box-container-and-quick-inventory', scale: 1.77, blur: 3, floor: '#645a50', at: [323, 207], to: '.inventory-slot' },
            { img: 'quick-inventory-hands', scale: 1.17, blur: 2, floor: '#201a28', at: [178, 95], to: '[data-hands] > .inventory-slot' },
        ],
        equipment: [{ img: 'equipment-open-and-hands', scale: 1.86, blur: 2, floor: '#000000', at: [16, 25], to: '.equipment-grid .inventory-slot' }],
        storage: [{ img: 'box-container-inside-box-container', scale: 1.87, blur: 2.1, floor: '#998f86', at: [180, 286], to: '[data-panel=toolbox]' }],
        pile: [{ img: 'many-items-on-same-spot', scale: 1.87, blur: 2.4, floor: '#968c83', at: [292, 307], to: '.pile-picker' }],
        search: [{ img: 'other-player-full-inventory', scale: 1.87, blur: 1.5, floor: '#9a8e82', at: [154, 167], to: '.character-paperdoll' }],
        radial: [{ img: 'item-interactions-wheel', scale: 1.87, blur: 0.8, floor: '#b0aa92', at: [681.5, 348], to: '.radial-interaction-menu', corner: 'c' }],
        // Same capture scale as the in-game shots; pinned at the selected tab's top-left.
        setup: [
            { img: 'character-setup-profile', tab: 'settings', scale: 1.87, blur: 1.35, at: [79, 274], to: '[data-tab=settings]' },
            { img: 'character-setup-appearance', tab: 'appearance', scale: 1.87, blur: 1.35, at: [72, 320], to: '[data-tab=appearance]' },
            { img: 'character-setup-hair', tab: 'hair', scale: 1.87, blur: 1.35, at: [177, 416], to: '[data-tab=hair]' },
        ],
    };

    // Hand nudges (drag, scale) per specimen and screenshot, in the specimen's own units; per browser only.
    const nudges = store.get('ref-nudges') || {};
    const nudgeKey = st => st.key + ':' + st.refs[st.i].img;
    const nudgeOf = st => nudges[nudgeKey(st)] || (nudges[nudgeKey(st)] = { dx: 0, dy: 0, s: 1 });
    const saveNudges = () => store.set('ref-nudges', nudges);
    let cmpAlpha = store.get('cmp-alpha');
    if (typeof cmpAlpha !== 'number') cmpAlpha = 0.6;
    let cmpLowRes = store.get('cmp-lowres') === true;

    // The low-res blur, in the UI's own units: reference px inside a specimen's zoom, canvas px on a stage.
    // (A filter length on an element is scaled by that element's zoom and transform, like any other length.)
    function setBlur(host) {
        const st = host._cmp;
        const ref = st.refs[st.i];
        host.style.setProperty('--cmp-blur', (ref.blur / ref.scale).toFixed(2) + 'px');
        if (ref.floor) host.style.setProperty('--cmp-floor', ref.floor);
        host.toggleAttribute('data-cmp-floor', !!ref.floor);
    }

    function cmpControl(cls, label, min, max, step, value, onInput) {
        const wrap = h('label', 'cmp-bar__ctl ' + cls);
        const inp = document.createElement('input');
        Object.assign(inp, { type: 'range', min, max, step, value });
        inp.addEventListener('input', () => onInput(Number(inp.value)));
        wrap.append(h('span', '', { text: label }), inp);
        return wrap;
    }

    function wireCompare() {
        $$('[data-cmp]').forEach(host => {
            const refs = CMP[host.dataset.cmp];
            if (!refs) return;
            const isStage = host.classList.contains('stage');
            const st = { key: host.dataset.cmp, refs, i: 0, mode: 'ui', isStage };
            host._cmp = st;
            host.dataset.cmpMode = 'ui';
            host.style.setProperty('--cmp-alpha', cmpAlpha);

            st.layer = h('div', isStage ? 'stage__ref' : 'cmp-ref');
            st.img = document.createElement('img');
            st.img.alt = '';
            st.img.draggable = false;
            st.img.addEventListener('load', () => placeRef(host));
            st.layer.append(st.img);
            (isStage ? $('.stage__viewport', host) : $('.spec__stage', host)).append(st.layer);

            const bar = h('div', 'cmp-bar');
            bar.append(h('span', '', { text: 'reference' }));
            const modes = h('div', 'cmp-bar__group');
            [['ui', 'ui only'], ['side', 'side by side'], ['overlay', 'overlay']].forEach(([m, text]) => {
                const b = h('button', 'chip-btn' + (m === 'ui' ? ' is-on' : ''), { text });
                b.type = 'button';
                b.addEventListener('click', () => {
                    $$('.chip-btn', modes).forEach(x => x.classList.toggle('is-on', x === b));
                    setMode(host, m);
                });
                modes.append(b);
            });
            bar.append(modes);
            if (refs.length > 1) {
                st.refChips = h('div', 'cmp-bar__group');
                refs.forEach((r, i) => {
                    const b = h('button', 'chip-btn' + (i === 0 ? ' is-on' : ''), { text: r.tab || r.img });
                    b.type = 'button';
                    b.addEventListener('click', () => setRef(host, i));
                    st.refChips.append(b);
                });
                bar.append(st.refChips);
            }
            bar.append(cmpControl('cmp-bar__ctl--overlay', 'ui opacity', 0, 1, 0.05, cmpAlpha, v => {
                cmpAlpha = v;
                store.set('cmp-alpha', v);
                $$('[data-cmp]').forEach(x => x.style.setProperty('--cmp-alpha', v));
                $$('.cmp-bar__ctl--overlay input').forEach(x => { x.value = v; });
            }));
            const scale = cmpControl('cmp-bar__ctl--tune', 'scale', 0.8, 1.2, 0.005, 1, v => {
                nudgeOf(st).s = v;
                saveNudges();
                placeRef(host);
            });
            st.scaleInput = $('input', scale);
            const reset = h('button', 'chip-btn cmp-bar__ctl cmp-bar__ctl--tune', { text: 'reset' });
            reset.type = 'button';
            reset.addEventListener('click', () => resetNudge(host));
            // Low res: blur the UI by the screenshot's own softness, so both sides are judged at the same resolution.
            const low = h('button', 'chip-btn' + (cmpLowRes ? ' is-on' : ''), { text: 'low res' });
            low.type = 'button';
            low.title = "Blur the UI by the screenshot's own softness";
            low.addEventListener('click', () => {
                cmpLowRes = !cmpLowRes;
                store.set('cmp-lowres', cmpLowRes);
                $$('[data-cmp]').forEach(x => {
                    x.toggleAttribute('data-cmp-lowres', cmpLowRes);
                    $$('.cmp-bar .chip-btn[title^="Blur"]', x).forEach(b => b.classList.toggle('is-on', cmpLowRes));
                });
            });
            host.toggleAttribute('data-cmp-lowres', cmpLowRes);
            bar.append(low, scale, reset, h('span', 'cmp-bar__hint', { text: 'drag the reference to nudge it · double-click resets' }));
            $('.spec__bar', host).after(bar);

            wireRefDrag(host);
            setRef(host, 0);
        });
    }

    function setMode(host, mode) {
        const st = host._cmp;
        st.mode = mode;
        host.dataset.cmpMode = mode;
        if (st.isStage) fitStage(host);
        // Measuring forces layout, so this runs straight away (no frame wait; frames pause in hidden tabs).
        layoutCompare(host);
    }

    function layoutCompare(host) {
        fitSide(host);
        anchorRoots().forEach(anchorAll);
        placeRef(host);
    }

    // Side by side gives the specimen half the stage; when it doesn't fit at the current zoom, both halves zoom out
    // together (the reference follows the specimen's zoom, so they stay comparable).
    function fitSide(host) {
        const st = host._cmp;
        if (!st || st.isStage || !shown(host)) return;
        host.style.removeProperty('--spec-zoom');
        if (st.mode !== 'side') return;
        const zoom = $('.spec__zoom', host);
        const stage = $('.spec__stage', host);
        host.dataset.cmpMode = 'fit'; // measure the specimen's own width: unconstrained, captions hidden
        const kz = cmpUnit(host);
        const left = zoom.getBoundingClientRect().left;
        const inFlow = Array.from(zoom.children).filter(c => getComputedStyle(c).position !== 'absolute');
        const natural = (Math.max(...inFlow.map(c => c.getBoundingClientRect().right)) - left) / kz;
        host.dataset.cmpMode = 'side';
        const room = stage.clientWidth / 2 - parseFloat(getComputedStyle(stage).paddingLeft) - 10;
        const global = parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--spec-zoom')) || 1;
        if (natural * global > room) host.style.setProperty('--spec-zoom', (room / natural).toFixed(3));
    }

    function setRef(host, i) {
        const st = host._cmp;
        st.i = i;
        if (st.refChips) $$('.chip-btn', st.refChips).forEach((b, j) => b.classList.toggle('is-on', j === i));
        st.scaleInput.value = (nudges[nudgeKey(st)] || {}).s || 1;
        setBlur(host);
        const src = REF_DIR + st.refs[i].img + '.jpg';
        if (!st.img.src.endsWith(src)) st.img.src = src;
        else placeRef(host);
    }

    function resetNudge(host) {
        const st = host._cmp;
        delete nudges[nudgeKey(st)];
        saveNudges();
        st.scaleInput.value = 1;
        placeRef(host);
    }

    // CSS px per nudge unit: reference px for a specimen, canvas px for a stage.
    const cmpUnit = host => {
        const root = host._cmp.isStage ? $('.stage__canvas', host) : $('.spec__zoom', host);
        return probe(root).getBoundingClientRect().width / 100;
    };

    function placeRef(host) {
        const st = host._cmp;
        if (!st || st.mode === 'ui' || !st.img.naturalWidth || !shown(host)) return;
        const ref = st.refs[st.i];
        const n = nudges[nudgeKey(st)] || { dx: 0, dy: 0, s: 1 };
        const put = (k, x, y) => Object.assign(st.img.style, { width: st.img.naturalWidth * k + 'px', left: x + 'px', top: y + 'px' });
        if (st.isStage) {
            // Canvas px; the reference layer carries the same scale transform as the canvas.
            const cv = $('.stage__canvas', host);
            const p = local($(ref.to, cv), cv);
            const k = 1 / ref.scale * n.s;
            const top = p.y - ref.at[1] * k + n.dy;
            put(k, p.x - ref.at[0] * k + n.dx, top);
            // A screenshot that runs past the 675 canvas: grow the reference layer and the viewport to show it all.
            const extra = Math.max(0, Math.ceil(top + st.img.naturalHeight * k - 675));
            if (extra !== st.extra) {
                st.extra = extra;
                fitStage(host);
            }
            return;
        }
        // CSS px relative to the stage; in side-by-side the reference pane mirrors the stage's left half.
        const S = $('.spec__stage', host).getBoundingClientRect();
        const kz = cmpUnit(host);
        const r = $(ref.to, $('.spec__zoom', host)).getBoundingClientRect();
        const c = ref.corner === 'c' ? 0.5 : 0;
        const k = kz / ref.scale * n.s;
        put(k, r.left - S.left + r.width * c - ref.at[0] * k + n.dx * kz, r.top - S.top + r.height * c - ref.at[1] * k + n.dy * kz);
    }

    function wireRefDrag(host) {
        const st = host._cmp;
        let drag = null;
        st.layer.addEventListener('pointerdown', e => {
            drag = { x: e.clientX, y: e.clientY, dx: nudgeOf(st).dx, dy: nudgeOf(st).dy, u: cmpUnit(host) };
            st.layer.setPointerCapture(e.pointerId);
            st.layer.style.cursor = 'grabbing';
            e.preventDefault();
        });
        st.layer.addEventListener('pointermove', e => {
            if (!drag) return;
            const n = nudgeOf(st);
            n.dx = drag.dx + (e.clientX - drag.x) / drag.u;
            n.dy = drag.dy + (e.clientY - drag.y) / drag.u;
            placeRef(host);
        });
        const end = () => {
            if (!drag) return;
            drag = null;
            st.layer.style.cursor = '';
            saveNudges();
        };
        st.layer.addEventListener('pointerup', end);
        st.layer.addEventListener('pointercancel', end);
        st.layer.addEventListener('dblclick', () => resetNudge(host));
    }

    // ══════════════════════════════════════════════════════════════════════
    // All elements: what each one is made of, read off the page
    // ══════════════════════════════════════════════════════════════════════
    // [family, [label, selector of the element that paints, class shown]]
    const PARTS = [
        ['slots and cells', [
            ['gear slot', '#el-slot .inventory-slot__well', '.inventory-slot'],
            ['hand, active', '#el-wide .inventory-slot--active .inventory-slot__well', '--wide --active'],
            ['hand', '#el-wide .inventory-slot:not(.inventory-slot--active) .inventory-slot__well', '--wide'],
            ['frame', '#el-frame .inventory-slot__well', '--frame'],
            ['frame, small', '#el-frame-sm .inventory-slot__well', '--frame --sm'],
            ['equipment frame', '#el-equipment .inventory-slot--frame:not(.inventory-slot--frameless) .inventory-slot__well', '--frame in .equipment-grid'],
            ['frame on the sheet', '#el-sheet .character-paperdoll__slot--belt .inventory-slot__well', '--frame --sm (sheet)'],
            ['storage cell', '#el-cell .storage-slot', '.storage-slot'],
            ['pile cell', '#el-pile .pile-picker__cell', '.pile-picker__cell'],
        ]],
        ['surfaces', [
            ['storage panel', '#el-panel .storage-panel', '.storage-panel'],
            ['search sheet', '#el-sheet .character-paperdoll', '.character-paperdoll'],
            ['pile plate', '#el-pile .pile-picker', '.pile-picker'],
        ]],
        ['radial', [
            ['disc', '#el-radial .radial-interaction-menu__disc', '__disc'],
            ['wedge', '#el-radial .radial-interaction-menu__wedge', '__wedge'],
            ['hub', '#el-radial .radial-interaction-menu__hub-plate', '__hub-plate'],
        ]],
        ['menu controls', [
            ['tab', '#el-tabs .menu-tab:not(.menu-tab--selected):not(.menu-tab--disabled)', '.menu-tab'],
            ['tab, selected', '#el-tabs .menu-tab--selected', '--selected'],
            ['tab, disabled', '#el-tabs .menu-tab--disabled', '--disabled'],
            ['segmented option', '#el-fields .menu-segmented__option:not(.menu-segmented__option--selected)', '.menu-segmented__option'],
            ['segmented, selected', '#el-fields .menu-segmented__option--selected', '--selected'],
            ['text field', '#el-fields .menu-text-field', '.menu-text-field'],
            ['dropdown', '#el-fields .menu-dropdown', '.menu-dropdown'],
            ['export code', '#el-fields .menu-code', '.menu-code'],
            ['slider track', '#el-fields .menu-slider__tracker', '.menu-slider__tracker'],
            ['slider handle', '#el-fields .menu-slider__dragger', '.menu-slider__dragger'],
            ['rule', '#el-title .screen-rule', '.screen-rule'],
            ['button', '#el-buttons .menu-button:not(.menu-button--secondary)', '.menu-button'],
            ['button, secondary', '#el-buttons .menu-button--secondary', '--secondary'],
            ['style tile', '#el-picks .style-tile:not(.style-tile--selected)', '.style-tile'],
            ['style tile, selected', '#el-picks .style-tile--selected', '--selected'],
            ['scroller', '#el-picks .menu-scroller', '.menu-scroller'],
            ['preview frame', '#el-buttons .preview-frame', '.preview-frame'],
        ]],
    ];

    // Every --ui-* colour token by its computed colour, so a painted colour can be named.
    function tokenColours() {
        const names = new Set();
        const take = st => { for (let i = 0; i < st.length; i++) if (st[i].startsWith('--ui-')) names.add(st[i]); };
        for (const sheet of document.styleSheets) {
            let rules;
            try { rules = sheet.cssRules; } catch { continue; }
            for (const r of rules) if (r.selectorText === ':root') take(r.style);
        }
        const root = getComputedStyle(document.documentElement);
        const tmp = document.createElement('div');
        document.body.append(tmp);
        const map = new Map();
        [...names].sort().forEach(n => {
            tmp.style.color = '';
            tmp.style.color = root.getPropertyValue(n).trim();
            if (!tmp.style.color) return;
            const c = getComputedStyle(tmp).color;
            if (!map.has(c)) map.set(c, n);
        });
        tmp.remove();
        return map;
    }

    function refreshParts(table) {
        const tokens = tokenColours();
        // Browsers snap borders to whole device pixels at the specimen zoom (a 3 px border reads 2.5). Measure at a zoom
        // where one reference px is exactly 4 device px, then put the zoom back; nothing paints in between.
        const zooms = $$('#elements .spec__zoom').map(z => [z, z.style.zoom]);
        zooms.forEach(([z]) => { z.style.zoom = String(4 / (window.devicePixelRatio || 1)); });
        const num = v => Math.round(parseFloat(v) * 2) / 2;
        const clear = c => !c || c === 'none' || c === 'transparent' || /rgba\([^)]*,\s*0\)$/.test(c);
        const colour = c => {
            const cell = h('span', 'parts__colour');
            const sw = h('span', 'parts__sw');
            sw.style.background = `linear-gradient(${c}, ${c}), #8a8782`;
            cell.append(sw, h('span', '', { text: tokens.get(c) || '' }), h('span', 'parts__raw', { text: c.replace(/\s+/g, '') }));
            return cell;
        };
        table.textContent = '';
        const head = h('tr');
        ['element', 'class', 'size', 'fill', 'edge', 'radius', 'opacity'].forEach(t => head.append(h('th', '', { text: t })));
        table.append(head);
        PARTS.forEach(([family, rows]) => {
            const g = h('tr', 'parts__group');
            const th = h('th', '', { text: family });
            th.colSpan = 7;
            g.append(th);
            table.append(g);
            rows.forEach(([label, sel, cls]) => {
                const el = $(sel);
                const tr = h('tr');
                const td = (content, c) => {
                    const cell = h('td', c || '');
                    if (content instanceof Node) cell.append(content);
                    else cell.textContent = content;
                    tr.append(cell);
                };
                td(label);
                td(cls, 'parts__cls');
                if (!el) { td('—'); table.append(tr); return; }
                const s = getComputedStyle(el);
                const w = num(s.width);
                const hh = num(s.height);
                td(`${w} × ${hh}`);
                const svg = el.namespaceURI === SVG_NS;
                const fill = svg ? s.fill : s.backgroundColor;
                td(!svg && s.backgroundImage.includes('gradient') ? 'gradient' : clear(fill) ? '—' : colour(fill));
                const bw = num(s.borderTopWidth);
                if (bw > 0 && !clear(s.borderTopColor)) {
                    const e = h('span');
                    e.append(h('span', 'parts__bw', { text: bw + ' px' }), colour(s.borderTopColor));
                    td(e);
                } else td('—');
                const r = Math.round(parseFloat(s.borderTopLeftRadius) * 100) / 100;
                td(s.borderTopLeftRadius.includes('%') || (r > 0 && r >= Math.min(w, hh) / 2 - 0.5) ? 'round' : String(r));
                const op = parseFloat(s.opacity);
                td(op < 1 ? String(op) : '');
                table.append(tr);
            });
        });
        zooms.forEach(([z, v]) => { z.style.zoom = v; });
    }

    // ══════════════════════════════════════════════════════════════════════
    // Map editor (B): MapEditorView's tree, drawn with the bible's parts
    // The palette is the real one (MapEditorCatalog.asset, MapEditorSubTabs); tile thumbnails are stand-ins (.me-thumb),
    // since most tiles have no icon sprite yet. The world behind is a drawn stand-in for the station.
    // ══════════════════════════════════════════════════════════════════════
    const DEPT = {
        command: '#1652b7', security: '#b20500', engineering: '#ffd200', medical: '#34e2ec',
        science: '#831fc5', cargo: '#d68739', service: '#4dd465', civilian: '#b9b9b9',
    };
    const meIcon = n => 'MapEditor/' + n + '.svg';
    const words = s => s.replace(/([a-z])([A-Z0-9])/g, '$1 $2').replace(/([A-Z])([A-Z][a-z])/g, '$1 $2').toLowerCase();

    // Tools: id, glyph, hint label, hotkey (drawn in the cell's corner) or key chips (hint only), popover it opens.
    const ME_TOOLS = [
        [{ id: 'construct', icon: 'construct', label: 'construct', key: '1' }, { id: 'select', icon: 'select', label: 'select', key: '2' },
            { id: 'dropper', icon: 'dropper', label: 'dropper', key: '3' }, { id: 'delete', icon: 'trash', label: 'delete', key: '4' },
            { id: 'room', icon: 'room', label: 'room', key: '5' }],
        [{ id: 'undo', icon: 'undo', label: 'undo', keys: ['CTRL', 'Z'] }, { id: 'redo', icon: 'redo', label: 'redo', keys: ['CTRL', 'Y'] }],
        [{ id: 'save', icon: 'saveAs', label: 'save map', keys: ['CTRL', 'S'], pop: 'save' },
            { id: 'load', icon: 'openMap', label: 'load map', keys: ['CTRL', 'SHIFT', 'O'], pop: 'maps' }],
    ];
    // The view bar, top left: what's shown and how.
    const ME_VIEW_TOOLS = [
        [{ id: 'layers', icon: 'layers', label: 'layers', pop: 'layers' }, { id: 'settings', icon: 'settings', label: 'settings', pop: 'settings' }],
        [{ id: 'atmos', icon: 'pipe', label: 'atmos view' }, { id: 'power', icon: 'bolt', label: 'power view' }],
        [{ id: 'check', iconKey: 'Heroicons/Outline/Exclamation', label: 'map check', pop: 'check' }],
    ];
    // Under the camera dial: the camera's own reset.
    const ME_CAMERA_TOOLS = [[{ id: 'reset', icon: 'resetPosition', label: 'reset view' }]];
    const ME_TOOL_TABS = { construct: 1, room: 1 };

    // Palette: [mode, [sub-tab, its glyph, entries]]. An entry is Name, Family>Variant,Variant, and =thumb:arg for a
    // stand-in thumbnail; underscores are spaces. Entries without a thumb show the sub-tab's glyph.
    const ME_MODES = [
        ['upper', [
            ['floors', 'grid', 'TileGrey=floor:#8b8e92 TileGreyDark=floor:#55585d TileWhite=floor:#c6c8ca TileWood=floor:#8a5b3a TileKitchen=checker TileBar=floor:#5b2b30 TileReinforced=floor:#6c6f74 TilePlating=plating TileGrass=floor:#4e8a3c TileDirt1=floor:#6d5139 FancyCarpetRed=floor:#8c2626'],
            ['walls & windows', 'bracket', 'SteelWall=wall:#4a4e55 SteelWallReinforced=wall:#383c42 SteelWindow=window SteelWindowReinforced=window SteelGirder=girder SteelGirderPlated=girder'],
            ['doors', 'door', 'Airlock>Standard,Glass=door:#9aa0a6 Command_Airlock>Standard,Glass=door:#1652b7 Security_Airlock>Standard,Glass=door:#b20500 Engineering_Airlock>Standard,Glass=door:#ffd200 Medical_Airlock>Standard,Glass=door:#34e2ec Science_Airlock>Standard,Glass=door:#831fc5 Supply_Airlock>Standard,Glass=door:#d68739 Atmospherics_Airlock>Standard,Glass=door:#3fb8b0 Maintenance_Airlock>Standard,Glass=door:#6e6e6e Chemistry_Airlock>Standard,Glass=door:#e0812a Civilian_Airlock>Standard,Glass=door:#b9b9b9 Genetics_Airlock>Standard,Glass=door:#5a8de0 Surgery_Airlock>Standard,Glass=door:#9fd8dc Virology_Airlock>Standard,Glass=door:#6fbf5a ExternalAirlock=door:#c0392b'],
            ['wall mounts', 'pin', 'APC AirAlarm FireAlarm LightBulbFixture LightSwitch=render:Rendered/LightSwitch LightTubeFixture Mirror PosterHorizontal PosterVertical'],
            ['furniture', 'cube', 'DinnerBooth JukeboxBlueGreen Locker LockerSecure Office_Chair>Standard,Blue,Grey Plant>Earth,Exotic Plastic_Chair>Standard,Blue,Grey SecurityLocker Sink TableFrameSteel TableFrameWood TableGlass TablePoker TableSteel TableWood WaterCooler'],
            ['machines', 'settings', 'AtmosInjector AtmosMeter Blender ChemicalDispenser Cryotube DisposalBin DisposalOutlet FuelTank Gibber MachineFrame Microwave NukeCart PacManGenerator PowerCellCharger SMES Scrubber ScrubberIntake SleeperBed SlotMachine SolarPanel SolarTrackingBeacon TableBoozeDispenser VendorHotDrinks VendorRobustSoftdrinks VendorSecTech VendorYouTool Vent WaterTank'],
            ['decals', 'layers', 'DepartmentCorner'],
            ['floor paint', 'grid', 'Unpainted=paint:none Command=paint:command Security=paint:security Engineering=paint:engineering Medical=paint:medical Science=paint:science Cargo=paint:cargo Service=paint:service Civilian=paint:civilian'],
        ]],
        ['lower', [
            ['subfloor', 'dots9', 'Plenum=plating Catwalk=catwalk Lattice=lattice'],
            ['cables', 'bolt', 'Cables=cable'],
            ['gas pipes', 'pipe', 'AtmosPipesSupply=pipe:#3d7fd6 AtmosPipesWaste=pipe:#d64a3d AtmosPipesMixed=pipe:#9b6fd6 Surface_Pipe>Standard,Blue,Cyan,Green,Orange,Purple,Red,Yellow=pipe:#9aa0a6 AtmosManifold3 AtmosManifold4 Filter>Left,Right Inline_Filter>Left,Right Mixer>Left,Right AtmosPumpVolume AtmosValveDigital AtmosValveManual GasPump'],
            ['disposal pipes', 'trash', 'DisposalPipes=pipe:#8a7a52'],
        ]],
        ['items', [
            ['tools', 'settings', 'Crowbar Hatchet KitchenKnife Multitool ScrewdriverBlue ToolboxBlue=render:Rendered/ToolboxNote Welder Wirecutters Wrench'],
            ['materials & parts', 'cube', 'ElectricalCable ElectricalWire FloorTile FloorTileBar FloorTileGreyDark FloorTileKitchen FloorTileWhite FloorTileWood GlassReinforcedSheet GlassShard GlassSheet LightBulb LightBulbBroken LightTube LightTubeBroken MatterBin MicroLaser PowerCell PowerCellAdmin SteelGirderUnbolted SteelReinforcedSheet SteelRod SteelSheet WoodLog WoodPlank'],
            ['clothing', 'box', 'Backpack FacewearGasMask GloveInsulated JumpsuitBotany JumpsuitEngineer JumpsuitGrey JumpsuitSecurity ShoeHiTops ShoeJackboots Sunglasses SyndicateHardsuit SyndicateHardsuitHelmet SyndicateMagboots Toolbelt TruckerCap'],
            ['medical', 'cube', 'BrutePatch BurnPatch Gauze HealthScanner MedicalPatch Medkit PortableDefibrillator'],
            ['food & drink', 'box', 'DonkPocket Mug Soda_Can>Beeps_Soda,Beepy_Lime,Canned_Air,Canned_Laughter,Cola,Duke_Purple_Tea,Tonic'],
            ['devices', 'bolt', 'Boombox FlashlightBlue Headset HeadsetSecurity IDCard NuclearAuthenticationDisk PDA SecurityIDCard SecurityPDA'],
            ['weapons', 'bolt', 'C20R C4 Flamethrower IonRifle LaserRifle M4 SecurityPistol Stechkin StunBaton SyndicateBomb SyndicateBombBeacon SyndicateMiniBomb Taser X4'],
            ['misc', 'dots9', 'BikeHorn Doodad EmergencyOxygenTank OxygenTank PlasmaTank Poster'],
        ]],
        ['scripts', [
            ['spawn points', 'pin', 'Assistant=marker:External/delapouite/person.svg:#40bfff Security_Officer=marker:External/delapouite/police-officer-head.svg:#40bfff Nuclear_Operatives=marker:External/lorc/radioactive.svg:#ff5940'],
            ['rooms', 'grid', 'Medbay=area:#34e2ec Bridge=area:#1652b7 Hallway=area:#b9b9b9 Engineering=area:#ffd200 Brig=area:#b20500'],
            ['starting air', 'pipe', 'Gas_Source=marker:MapEditor/pipe.svg:#4de673 Line_Fill=marker:MapEditor/pipe.svg:#ffbf40 Room_Air=marker:MapEditor/pipe.svg:#b380ff'],
        ]],
    ].map(([mode, tabs]) => ({
        mode,
        tabs: tabs.map(([label, glyph, list]) => ({
            label, glyph,
            entries: list.split(' ').map(tok => {
                const [head, thumb = ''] = tok.split('=');
                const [name, vars] = head.split('>');
                return {
                    // Floors drop their asset names' "Tile" prefix: "TileGreyDark" reads "grey dark".
                    label: name.includes('_') ? name.replace(/_/g, ' ').toLowerCase() : words(name).replace(/^tile /, ''),
                    variants: vars ? vars.split(',').map(v => v.replace(/_/g, ' ').toLowerCase()) : null,
                    thumb,
                };
            }),
        })),
    }));

    const ME_LAYERS = ['subfloor', 'turfs', 'furniture', 'wall mounts', 'cables', 'gas pipes', 'disposals', 'floor decals', 'items', 'scripts', 'areas'];
    const ME_SETTINGS = [['auto subfloor', true], ['always power lights', false], ['player invincible', true], ['debug overlay', false]];
    const ME_CAMERA = [['field of view', 0.42, 50, 110, '°'], ['zoom speed', 0.45, 1, 10, ''], ['rotation speed', 0.34, 1, 10, '']];
    const ME_MAPS = [['classic-ministation', 'autosave'], ['metastation', ''], ['box-station', ''], ['test-atmos', '']];
    const ME_ISSUES = [
        { sev: 'error', title: 'offered jobs missing spawn markers', path: 'jobs', body: 'every offered job needs an authored spawn marker. add them under scripts → spawn points.', acts: ['show in palette'] },
        { sev: 'warning', title: 'air alarm at (14, 9) faces no room', path: 'rooms', body: 'so it controls nothing. check which way it faces.', acts: ['locate', 'ignore'] },
        { sev: 'warning', title: 'no apc powers this space', path: 'power', body: 'add an apc to that space, or paint it into a room under scripts → rooms.', acts: ['locate', 'ignore'] },
    ];
    // The camera dial's four verbs, clockwise from the top.
    const DIAL = [
        { id: 'zoom-in', icon: 'zoomIn', label: 'zoom in' }, { id: 'rotate-right', icon: 'rotateRight', label: 'rotate right' },
        { id: 'zoom-out', icon: 'zoomOut', label: 'zoom out' }, { id: 'rotate-left', icon: 'rotateLeft', label: 'rotate left' },
    ];

    const me = {
        mode: 0, tabs: [0, 0, 0, 0], tool: 'construct', picked: '0/0/0', variants: {}, search: '',
        size: 'normal', undo: 3, redo: 0, pop: null, view: null, rot: 0, zoom: 0,
        layers: Object.fromEntries(ME_LAYERS.map(l => [l, l !== 'scripts' && l !== 'areas'])),
        settings: Object.fromEntries(ME_SETTINGS), check: { filter: 'all', open: -1, ignored: new Set() },
    };

    // ── The camera dial: the radial menu's disc, hub and wedge at 80 px. Also built by the states tab (data-point). ──
    function buildCameraDial(el) {
        const d = h('div', 'map-editor-camera-dial');
        d.append(h('div', 'map-editor-camera-dial__ring'));
        const cs = getComputedStyle(document.documentElement);
        const size = parseFloat(cs.getPropertyValue('--ui-dial'));
        const hub = parseFloat(cs.getPropertyValue('--ui-dial-hub'));
        const wedge = radialWedge(size, hub / 2 - 1, 90);
        wedge.setAttribute('class', 'map-editor-camera-dial__wedge');
        d.append(wedge);
        const r = (hub / 2 + size / 2) / 2 / size * 100; // the middle of the band, as % of the dial
        DIAL.forEach((v, i) => {
            const a = i * Math.PI / 2;
            const b = h('div', 'map-editor-camera-dial__btn', { style: { left: 50 + Math.sin(a) * r + '%', top: 50 - Math.cos(a) * r + '%' } });
            b.title = v.label;
            b.append(h('div', 'map-editor-camera-dial__btn-icon', { icon: meIcon(v.icon) }));
            d.append(b);
        });
        const open = h('div', 'map-editor-camera-dial__open');
        open.title = 'camera options';
        open.append(h('div', 'map-editor-camera-dial__open-icon', { icon: meIcon('camera') }));
        d.append(open);
        const point = i => {
            d.classList.toggle('map-editor-camera-dial--pointing', i >= 0);
            d.dataset.pointing = i >= 0 ? DIAL[i].id : '';
            if (i >= 0) wedge.style.rotate = i * 90 + 'deg';
        };
        const fixed = DIAL.findIndex(v => v.label === el.dataset.point);
        if (el.dataset.point) point(fixed);
        else {
            // The quadrant under the pointer, on the band (not the hub): the whole band is the button, as on the radial.
            d.addEventListener('pointermove', e => {
                const b = d.getBoundingClientRect();
                const dx = e.clientX - b.left - b.width / 2;
                const dy = e.clientY - b.top - b.height / 2;
                const rr = Math.hypot(dx, dy) / (b.width / 2);
                point(rr < hub / size || rr > 1 ? -1 : Math.round(Math.atan2(dx, -dy) / (Math.PI / 2) + 4) % 4);
            });
            d.addEventListener('pointerleave', () => point(-1));
        }
        el.replaceWith(d);
        return d;
    }

    const ME_CARET = { right: 'left', left: 'right', down: 'up' }; // which edge of the plate the caret sits on

    function meStrip(group, opens) {
        const s = h('div', 'storage-panel map-editor-toolbar-strip' + (opens === 'down' ? ' map-editor-toolbar-strip--horizontal' : ''));
        group.forEach(t => {
            const wrap = h('div', 'map-editor-toolbar-btn-wrap');
            const btn = h('div', 'storage-slot map-editor-icon-btn');
            btn.dataset.tool = t.id;
            btn.append(h('div', 'map-editor-icon-btn__icon', { icon: t.iconKey || meIcon(t.icon) }));
            if (t.key) {
                btn.append(h('div', 'map-editor-icon-btn__key', { text: t.key }));
                btn.classList.add('map-editor-icon-btn--tool-' + t.id); // colour coding
            }
            const hint = h('div', 'map-editor-toolbar-hint map-editor-toolbar-hint--opens-' + opens);
            hint.append(h('div', 'map-editor-caret map-editor-caret--' + ME_CARET[opens]));
            hint.append(h('div', 'map-editor-toolbar-hint__label', { text: t.label }));
            (t.keys || (t.key ? [t.key] : [])).forEach(k => hint.append(h('div', 'map-editor-key', { text: k })));
            wrap.append(btn, hint);
            s.append(wrap);
        });
        return s;
    }

    function meEntry(key) {
        const [m, t, i] = key.split('/').map(Number);
        const tab = ME_MODES[m].tabs[t];
        return { m, t, i, tab, e: tab.entries[i] };
    }

    // The slot's icon: a real render, a department colour, a tinted marker glyph, a stand-in thumbnail, or the
    // sub-tab's glyph at the empty-slot alpha.
    function meIconOf(e, tab, variant) {
        const [kind, a, b] = e.thumb.split(':');
        if (kind === 'render') return h('div', 'map-editor-slot__icon', { icon: a });
        if (kind === 'paint') {
            const p = h('div', 'map-editor-slot__icon map-editor-slot__icon--paint');
            if (a === 'none') p.classList.add('me-unpainted');
            else p.style.backgroundColor = DEPT[a];
            return p;
        }
        if (kind === 'marker') {
            // web-only tint (mask). USS: the glyph as background-image, -unity-background-image-tint-color: <colour>.
            const p = h('div', 'map-editor-slot__icon map-editor-slot__icon--marker me-tint');
            p.style.backgroundColor = b;
            p.style.webkitMaskImage = p.style.maskImage = cssUrl(a);
            return p;
        }
        if (kind) {
            const p = h('div', 'me-thumb me-thumb--' + kind);
            if (a) p.style.setProperty('--c', a);
            if (kind === 'pipe' && variant && variant !== 'standard') p.style.setProperty('--c', variant);
            if (kind === 'door' && variant === 'glass') p.classList.add('me-thumb--glass');
            return p;
        }
        return h('div', 'map-editor-slot__icon map-editor-slot__icon--glyph', { icon: meIcon(tab.glyph) });
    }

    function meSlot(key) {
        const { tab, e } = meEntry(key);
        const vi = me.variants[key] || 0;
        const variant = e.variants ? e.variants[vi] : null;
        const s = h('div', 'map-editor-slot');
        s.dataset.entry = key;
        s.title = variant ? `${e.label}: ${variant} (V / Shift+V for the next variant)` : e.label;
        s.classList.toggle('map-editor-slot--active', me.picked === key);
        const frame = h('div', 'map-editor-slot__icon-frame');
        frame.append(meIconOf(e, tab, variant));
        s.append(frame);
        const label = h('div', 'map-editor-slot__label', { text: e.label });
        if (variant) {
            s.classList.add('map-editor-slot--family');
            frame.append(h('div', 'map-editor-slot__variant', { text: variant }));
            frame.append(h('div', 'map-editor-slot__badge', { text: `${vi + 1}/${e.variants.length}` }));
            [['previous', '‹', -1], ['next', '›', 1]].forEach(([cls, glyph, step]) => {
                const c = h('div', 'map-editor-slot__cycle map-editor-slot__cycle--' + cls, { text: glyph });
                c.dataset.cycle = step;
                c.title = step < 0 ? 'previous variant (Shift+V)' : 'next variant (V)';
                frame.append(c);
            });
        }
        s.append(label);
        return s;
    }

    function meRenderLibrary(root) {
        const mode = ME_MODES[me.mode];
        const bottom = $('.map-editor-bottom-row', root);
        bottom.classList.toggle('map-editor-bottom-row--min', me.size === 'min');
        bottom.classList.toggle('map-editor-bottom-row--max', me.size === 'max');
        // Colour coding: the open category colours the library's lines.
        ME_MODES.forEach((m, i) => bottom.classList.toggle('map-editor-bottom-row--' + m.mode, i === me.mode));
        $$('.map-editor-mode-tab', root).forEach((t, i) => t.classList.toggle('menu-tab--selected', i === me.mode));
        $$('[data-size]', root).forEach(b => b.classList.toggle('map-editor-header-icon-btn--active', b.dataset.size === me.size));
        const row = $('.map-editor-subcat-row', root);
        row.replaceChildren(...mode.tabs.map((t, i) => {
            const tab = h('div', 'map-editor-subcat-tab', { text: t.label });
            tab.dataset.subtab = i;
            tab.classList.toggle('map-editor-subcat-tab--active', !me.search && i === me.tabs[me.mode]);
            return tab;
        }));
        const grid = $('.map-editor-grid', root);
        const q = me.search.trim().toLowerCase();
        let keys;
        if (q) {
            keys = [];
            ME_MODES.forEach((m, mi) => m.tabs.forEach((t, ti) => t.entries.forEach((e, ei) => {
                if ((e.label + ' ' + (e.variants || []).join(' ')).includes(q)) keys.push(`${mi}/${ti}/${ei}`);
            })));
        } else {
            const ti = me.tabs[me.mode];
            keys = mode.tabs[ti].entries.map((_, ei) => `${me.mode}/${ti}/${ei}`);
        }
        grid.replaceChildren(...(keys.length ? keys.map(meSlot)
            : [h('div', 'map-editor-stub-message', { text: q ? `no matches for "${q}" in any tab.` : 'nothing here yet.' })]));
    }

    // The hint bar's text, as MapEditorViewModel.UpdateHint words it; [KEY] draws as a key chip.
    function meHintText() {
        const { tab, e } = meEntry(me.picked);
        const variant = e.variants ? ' · [V] variant' : '';
        // Shorter than the shipped strings, so they fit the bar beside Map Check.
        if (me.tool === 'select') return 'select: click a tile to inspect it';
        if (me.tool === 'dropper') return 'dropper: click a placed object to copy it';
        if (me.tool === 'delete') return `delete ${ME_MODES[me.mode].tabs[me.tabs[me.mode]].label}: drag a line, [SHIFT] drag a rectangle`;
        if (me.tool === 'room') return 'room: steel wall and grey floor · drag a rectangle';
        if (tab.label === 'floor paint') return `${e.label}: click a square, [SHIFT] drag a rectangle, [CTRL] erases`;
        if (tab.label === 'wall mounts') return 'point at a wall face to place it · [R] turns it' + variant;
        if (tab.label === 'doors') return 'click a wall or a gap in one · [R] turns the door' + variant;
        if (ME_MODES[meEntry(me.picked).m].mode === 'items') return 'click to place the item' + variant;
        if (['floors', 'subfloor'].includes(tab.label)) return 'drag places a line · [SHIFT] drag fills a rectangle' + variant;
        return 'drag places a line · [SHIFT] drag fills a rectangle · [R] turns' + variant;
    }

    function meRenderHint(root) {
        const bar = $('.map-editor-hint', root);
        bar.replaceChildren(...meHintText().split(/(\[[A-Z]+\])/).filter(Boolean).map(part => /^\[[A-Z]+\]$/.test(part)
            ? h('div', 'map-editor-key', { text: part.slice(1, -1) })
            : h('span', 'map-editor-hint__text', { text: part.replace(/^ | $/g, ' ') })));
    }

    function meRenderTools(root) {
        $$('[data-tool]', root).forEach(b => {
            const id = b.dataset.tool;
            b.classList.toggle('map-editor-icon-btn--active', id === me.tool || id === me.view || (!!me.pop && id === meTools[me.pop]));
            b.classList.toggle('map-editor-icon-btn--disabled', (id === 'undo' && !me.undo) || (id === 'redo' && !me.redo));
        });
        $('.map-editor-camera-dial__open', root).classList.toggle('map-editor-camera-dial__open--active', me.pop === 'camera');
        // Colour coding: the tool colours the hint bar's edge.
        ME_TOOLS[0].forEach(t => root.classList.toggle('map-editor-root--tool-' + t.id, t.id === me.tool));
        const live = meLiveIssues();
        const worst = live.some(x => x.sev === 'error') ? 'map-check__danger' : live.length ? 'map-check__warning' : '';
        const cell = $('[data-tool="check"]', root);
        let dot = $('.map-editor-icon-btn__alert', cell);
        if (!dot) cell.append(dot = h('div', 'map-editor-icon-btn__alert'));
        dot.className = 'map-editor-icon-btn__alert ' + worst;
        dot.style.display = worst ? '' : 'none';
    }
    const meTools = { save: 'save', maps: 'load', layers: 'layers', settings: 'settings', check: 'check' };

    const meLiveIssues = () => ME_ISSUES.map((x, i) => ({ ...x, i })).filter(x => !me.check.ignored.has(x.i));

    function meRenderCheck(root) {
        const panel = $('.map-check', root);
        if (!panel) return; // closed
        const live = meLiveIssues();
        const errors = live.filter(x => x.sev === 'error').length;
        const warnings = live.length - errors;
        const c = me.check;
        const badge = (n, cls, text) => {
            const b = h('div', 'map-check__badge');
            b.append(h('div', 'map-check__dot ' + cls), document.createTextNode(text));
            return n ? b : null;
        };
        const head = h('div', 'map-check__header');
        head.append(h('div', 'map-check__title', { text: 'map check' }));
        [badge(errors, 'map-check__danger', `${errors} ${errors === 1 ? 'error' : 'errors'}`),
            badge(warnings, 'map-check__warning', String(warnings)),
            badge(!live.length, 'map-check__success', 'clear')].forEach(b => b && head.append(b));
        const close = h('div', 'map-editor-popover-close', { text: '×' });
        close.dataset.close = '';
        head.append(close);
        const filters = h('div', 'menu-segmented map-check__filters');
        [['all', 'all', live.length], ['error', 'errors', errors], ['warning', 'warnings', warnings]].forEach(([k, label, n]) => {
            const o = h('div', 'menu-segmented__option', { text: `${label} ${n}` });
            o.dataset.filter = k;
            o.classList.toggle('menu-segmented__option--selected', c.filter === k);
            filters.append(o);
        });
        const list = h('div', 'map-check__issues');
        live.filter(x => c.filter === 'all' || x.sev === c.filter).forEach(x => {
            const row = h('div', 'map-check__issue' + (c.open === x.i ? ' map-check__issue--open' : ''));
            row.dataset.issue = x.i;
            row.append(h('div', 'map-check__dot ' + (x.sev === 'error' ? 'map-check__danger' : 'map-check__warning')));
            const body = h('div', 'map-check__issue-body');
            body.append(h('div', 'map-check__issue-text', { text: x.title }));
            body.append(h('div', 'map-check__issue-path', { text: c.open === x.i ? x.body : x.path }));
            const acts = h('div', 'map-check__actions');
            x.acts.forEach(a => {
                const b = h('div', 'map-check__action', { text: a });
                b.dataset.act = a;
                acts.append(b);
            });
            body.append(acts);
            row.append(body);
            list.append(row);
        });
        if (!list.children.length) list.append(h('div', 'map-check__issue-path', { text: 'no issues.', style: { padding: '8px 12px' } }));
        const foot = h('div', 'map-check__footer' + (errors ? ' map-check__footer--danger' : ''),
            { text: errors ? 'errors block round start.' : 'map is playable.' });
        panel.replaceChildren(head, filters, list, foot);
    }

    // ── Popovers ──
    const ME_POP_TITLES = { layers: 'layer view mode', settings: 'map editor settings', camera: 'camera options', maps: 'map selection', save: 'save map' };

    function meSwitchRow(label, on, data) {
        const row = h('div', 'map-editor-switch-row');
        row.append(h('div', 'map-editor-switch-row__label', { text: label }));
        const sw = h('div', 'map-editor-switch' + (on ? ' map-editor-switch--on' : ''));
        Object.assign(sw.dataset, data);
        sw.append(h('div', 'map-editor-switch__thumb'));
        row.append(sw);
        return row;
    }

    function meSliderValue([, , lo, hi, unit], v) {
        return Math.round(lo + (hi - lo) * v) + unit;
    }

    function meBuildPopover(key) {
        if (key === 'check') {
            // Map Check's panel is the popover's window; meRenderCheck fills it.
            const pop = h('div', 'map-editor-popover map-editor-popover--check');
            pop.dataset.pop = key;
            pop.append(h('div', 'storage-panel map-editor-popover-window map-check'));
            return pop;
        }
        const pop = h('div', 'map-editor-popover' + (key === 'maps' || key === 'save' ? ' map-editor-popover--wide' : ''));
        pop.dataset.pop = key;
        const win = h('div', 'storage-panel map-editor-popover-window');
        const head = h('div', 'map-editor-popover-window__header');
        head.append(h('div', 'map-editor-window__title', { text: ME_POP_TITLES[key] }));
        const close = h('div', 'map-editor-popover-close', { text: '×' });
        close.dataset.close = '';
        head.append(close);
        const body = h('div', 'map-editor-popover-window__body');
        if (key === 'layers') ME_LAYERS.forEach(l => body.append(meSwitchRow(l, me.layers[l], { layer: l })));
        if (key === 'settings') ME_SETTINGS.forEach(([l]) => body.append(meSwitchRow(l, me.settings[l], { setting: l })));
        if (key === 'camera') ME_CAMERA.forEach(row => {
            const r = h('div', 'map-editor-slider-row');
            const top = h('div', 'map-editor-slider-row__header');
            const value = h('div', 'map-editor-slider-row__value', { text: meSliderValue(row, row[1]) });
            top.append(h('div', 'map-editor-slider-row__label', { text: row[0] }), value);
            const sl = h('div', 'menu-slider');
            sl.dataset.value = row[1];
            sl.append(h('div', 'menu-slider__tracker'), h('div', 'menu-slider__fill'), h('div', 'menu-slider__dragger'));
            // wireSliders moves the dragger; this keeps the value label in step.
            const sync = () => { row[1] = Number(sl.dataset.value); value.textContent = meSliderValue(row, row[1]); };
            sl.addEventListener('pointerdown', sync);
            sl.addEventListener('pointermove', sync);
            r.append(top, sl);
            body.append(r);
        });
        if (key === 'maps' || key === 'save') {
            if (key === 'save') {
                const field = h('div', 'menu-field');
                field.style.marginBottom = '10px';
                const input = h('input', 'menu-text-field');
                input.placeholder = 'Map name';
                input.value = 'classic-ministation';
                input.spellcheck = false;
                field.append(input);
                body.append(field);
                body.append(h('div', 'map-editor-map-badge', { text: 'or save over:', style: { marginBottom: '2px' } }));
            }
            ME_MAPS.forEach(([name, badge]) => {
                const row = h('div', 'map-editor-map-row');
                row.dataset.map = name;
                row.append(h('div', 'map-editor-map-name', { text: name }), h('div', 'map-editor-map-badge', { text: badge }));
                body.append(row);
            });
            // The popover's one action, so the primary plate: the secondary one is lost on the dark floating surface.
            const btn = h('div', 'menu-button');
            btn.dataset.popAct = key === 'maps' ? 'new map' : 'save';
            btn.style.alignSelf = 'flex-end';
            btn.append(h('div', 'menu-button__tip'), h('div', 'menu-button__text', { text: key === 'maps' ? 'new map' : 'save' }));
            body.append(btn);
        }
        win.append(head, body);
        pop.append(win);
        return pop;
    }

    // Beside its button, top-aligned to it, with a caret on the button's centre: left-column popovers open to the
    // right, the rest to the left (MapEditorView.PlacePopover). Kept inside the screen.
    function mePlacePopover(root, pop, anchor, side) {
        const cv = $('#me-canvas');
        const a = local(anchor, cv);
        const edge = local(anchor.closest('.map-editor-toolbar-strip') || anchor, cv);
        const w = pop.offsetWidth;
        const hgt = pop.offsetHeight;
        const caret = h('div', 'map-editor-caret map-editor-caret--' + ME_CARET[side]);
        if (side === 'down') {
            // Under the bar, centred on the button where it fits; the caret stays on the button's centre.
            const left = Math.max(16, Math.min(a.x + a.w / 2 - w / 2, 1200 - 16 - w));
            pop.style.top = edge.y + edge.h + 22.5 + 'px';
            pop.style.left = left + 'px';
            caret.style.left = a.x + a.w / 2 - left + 'px';
        } else {
            const top = Math.max(16, Math.min(a.y - 6, 675 - 16 - hgt));
            pop.style.top = top + 'px';
            pop.style.left = (side === 'right' ? edge.x + edge.w + 22.5 : edge.x - 22.5 - w) + 'px';
            caret.style.top = a.y + a.h / 2 - top + 'px';
        }
        pop.append(caret);
    }

    function meOpenPopover(root, key, anchor) {
        $$('.map-editor-popover', root).forEach(p => p.remove());
        me.pop = me.pop === key ? null : key;
        if (me.pop) {
            const pop = meBuildPopover(key);
            $('.me-popover-layer', root).append(pop);
            if (key === 'check') meRenderCheck(root);
            $$('.menu-slider', pop).forEach(sl => {
                setSlider(sl, Number(sl.dataset.value));
                const fromEvent = e => {
                    const r = sl.getBoundingClientRect();
                    setSlider(sl, (e.clientX - r.left) / r.width);
                };
                sl.addEventListener('pointerdown', e => { sl.setPointerCapture(e.pointerId); sl.classList.add('menu-slider--dragging'); fromEvent(e); }, true);
                sl.addEventListener('pointermove', e => { if (sl.hasPointerCapture(e.pointerId)) fromEvent(e); }, true);
                sl.addEventListener('pointerup', () => sl.classList.remove('menu-slider--dragging'));
            });
            mePlacePopover(root, pop, anchor, { camera: 'left', layers: 'right', settings: 'right', check: 'right' }[key] || 'down');
        }
        meRenderTools(root);
    }

    // ── Toast ──
    let meToastTimer = 0;
    function meToast(root, text) {
        const t = $('.map-editor-toast', root);
        t.textContent = text;
        t.classList.add('map-editor-toast--shown');
        clearTimeout(meToastTimer);
        meToastTimer = setTimeout(() => t.classList.remove('map-editor-toast--shown'), 2200);
    }

    // ══ The world: a drawn stand-in for the station under the free camera ══
    // ' ' space, '.' floor, '#' wall, 'W' window, 'D' airlock, 'p' plating, '+' lattice; m c e s = floor painted
    // medical, command, engineering, security (every square of the tile).
    const ME_MAP = [
        '',
        '   ##W#W###    ###W#W#W###',
        '   #mm.mmm#    #ccc...ccc#',
        '   #m.....#    #c.......c#',
        '   W......#    W.........W',
        '   #m.....#    #c.......c#',
        '   #mm.mmm#    #ccc...ccc#',
        '   ####D#########D####D###',
        '   #.....................#',
        '   W.....................W',
        '   ####D#####D########D###',
        '   #eeeeee#  ppppppp #sss#',
        '   #e....e#  ppppppp #s.s#',
        '   #eeeeee#  +++++++ #sss#',
        '   ########          #####',
    ];
    const ME_T = 40;
    const ME_AREAS = [['medbay', 4, 2, 6, 5, DEPT.medical], ['bridge', 16, 2, 9, 5, DEPT.command], ['hallway', 4, 8, 21, 2, DEPT.civilian],
        ['engineering', 4, 11, 6, 3, DEPT.engineering], ['brig', 22, 11, 3, 3, DEPT.security]];
    const ME_PAINT = { m: DEPT.medical, c: DEPT.command, e: DEPT.engineering, s: DEPT.security };
    // Ghosts per tool, in tiles: [x, y, w, h]. The construct ghost is a Shift-drag fill on the plating.
    const ME_GHOST = [13, 11, 7, 3];

    function meWorldSvg() {
        const at = (r, c) => (ME_MAP[r] || '')[c] || ' ';
        const isWall = ch => ch === '#' || ch === 'W';
        const out = { space: [], subfloor: [], cables: [], pipes: [], turfs: [], decals: [], mounts: [], areas: [], scripts: [] };
        // Stars, from a fixed seed so the sky doesn't change between loads.
        let seed = 7;
        const rnd = () => (seed = (seed * 16807) % 2147483647) / 2147483647;
        for (let i = 0; i < 140; i++) out.space.push(`<circle cx="${(rnd() * 1200).toFixed(1)}" cy="${(rnd() * 675).toFixed(1)}" r="${(0.4 + rnd() * 0.9).toFixed(2)}" fill="#fff" opacity="${(0.15 + rnd() * 0.5).toFixed(2)}"/>`);
        ME_MAP.forEach((line, r) => [...line].forEach((ch, c) => {
            const x = c * ME_T;
            const y = r * ME_T;
            if (ch === ' ') return;
            if (ch === '+') {
                out.subfloor.push(`<path d="M${x} ${y}L${x + ME_T} ${y + ME_T}M${x + ME_T} ${y}L${x} ${y + ME_T}M${x} ${y + 20}H${x + ME_T}" stroke="#6b7078" stroke-width="2" opacity="0.7"/>`);
                return;
            }
            // Plating under everything that stands on the grid.
            out.subfloor.push(`<rect x="${x}" y="${y}" width="${ME_T}" height="${ME_T}" fill="#3d4147"/><rect x="${x + 1}" y="${y + 1}" width="${ME_T - 2}" height="${ME_T - 2}" fill="none" stroke="#2c2f34" stroke-width="2"/>`
                + [[6, 6], [34, 6], [6, 34], [34, 34]].map(([dx, dy]) => `<circle cx="${x + dx}" cy="${y + dy}" r="1.6" fill="#596068"/>`).join(''));
            if (ch === 'p') return;
            if (isWall(ch)) {
                out.turfs.push(`<rect x="${x}" y="${y}" width="${ME_T}" height="${ME_T}" fill="#30343a"/><rect x="${x + 3}" y="${y + 3}" width="${ME_T - 6}" height="${ME_T - 6}" fill="#4d525a"/>`);
                if (ch === 'W') {
                    const across = isWall(at(r, c - 1)) || isWall(at(r, c + 1));
                    out.turfs.push(across ? `<rect x="${x}" y="${y + 14}" width="${ME_T}" height="12" fill="#6fb6d6" opacity="0.75"/>`
                        : `<rect x="${x + 14}" y="${y}" width="12" height="${ME_T}" fill="#6fb6d6" opacity="0.75"/>`);
                }
                return;
            }
            // A floor tile: 2 × 2 squares, as the floor shader draws it; painted tiles take a department colour.
            const paint = ME_PAINT[ch];
            for (let q = 0; q < 4; q++) {
                const sx = x + (q % 2) * 20;
                const sy = y + (q >> 1) * 20;
                out.turfs.push(`<rect x="${sx}" y="${sy}" width="20" height="20" fill="${paint ? 'url(#me-paint-' + ch + ')' : '#7f8287'}"/><path d="M${sx + 1} ${sy + 19}V${sy + 1}H${sx + 19}" stroke="#fff" stroke-opacity="0.13" fill="none"/><path d="M${sx + 1} ${sy + 19}H${sx + 19}V${sy + 1}" stroke="#000" stroke-opacity="0.2" fill="none"/>`);
            }
            if (ch === 'D') {
                const across = isWall(at(r, c - 1)) && isWall(at(r, c + 1));
                out.turfs.push(across
                    ? `<rect x="${x}" y="${y + 9}" width="${ME_T}" height="22" fill="#8e949b"/><rect x="${x}" y="${y + 9}" width="${ME_T}" height="4" fill="#d9b23a"/><path d="M${x + 20} ${y + 9}V${y + 31}" stroke="#3a3e44" stroke-width="2"/>`
                    : `<rect x="${x + 9}" y="${y}" width="22" height="${ME_T}" fill="#8e949b"/><rect x="${x + 9}" y="${y}" width="4" height="${ME_T}" fill="#d9b23a"/><path d="M${x + 9} ${y + 20}H${x + 31}" stroke="#3a3e44" stroke-width="2"/>`);
            }
        }));
        // Cables and pipes, under the floors: they show on the plating, or everywhere with turfs off.
        out.cables.push('<path d="M170 328H990M540 328V500H780M290 328V110M900 328V500" stroke="#e8c547" stroke-width="3" fill="none"/>');
        out.pipes.push('<path d="M170 344H990M780 344V110" stroke="#3d7fd6" stroke-width="5" fill="none"/><path d="M170 360H990M250 360V110M620 360V470" stroke="#d64a3d" stroke-width="5" fill="none"/>');
        // Wall mounts: light fixtures on the inner faces, an APC in the hallway.
        [[6, 1, 's'], [20, 1, 's'], [10, 8, 'n'], [18, 8, 'n'], [6, 14, 'n']].forEach(([c, r, face]) => {
            const x = c * ME_T + 10;
            const y = face === 's' ? (r + 1) * ME_T - 1 : r * ME_T - 5;
            out.mounts.push(`<rect x="${x}" y="${y}" width="20" height="6" rx="1" fill="#f3ead2"/>`);
        });
        out.mounts.push('<rect x="560" y="315" width="16" height="10" fill="#2b2f35" stroke="#15171a"/><circle cx="572" cy="320" r="2" fill="#7ee07a"/>');
        // Areas: each room's tint and name.
        ME_AREAS.forEach(([name, c, r, w, hh, col]) => out.areas.push(`<rect x="${c * ME_T}" y="${r * ME_T}" width="${w * ME_T}" height="${hh * ME_T}" fill="${col}" fill-opacity="0.22" stroke="${col}" stroke-width="2"/><text x="${c * ME_T + 8}" y="${r * ME_T + 18}" fill="#fff" font-family="Kode Mono, monospace" font-size="12">${name}</text>`));
        // Scripts: job spawn pins (blue) and an operative pin (red).
        [[8, 8, '#40bfff'], [13, 9, '#40bfff'], [21, 8, '#ff5940']].forEach(([c, r, col]) => out.scripts.push(`<circle cx="${c * ME_T + 20}" cy="${r * ME_T + 20}" r="11" fill="${col}" fill-opacity="0.35" stroke="${col}" stroke-width="2"/><circle cx="${c * ME_T + 20}" cy="${r * ME_T + 16}" r="3" fill="${col}"/><path d="M${c * ME_T + 14} ${r * ME_T + 27}a6 6 0 0 1 12 0" fill="${col}"/>`));
        const paints = Object.entries(ME_PAINT).map(([k, col]) => `<pattern id="me-paint-${k}" width="20" height="20" patternUnits="userSpaceOnUse"><rect width="20" height="20" fill="#7f8287"/><rect width="20" height="20" fill="${col}" opacity="0.45"/></pattern>`).join('');
        const g = (layer, items) => `<g data-layer="${layer}">${items.join('')}</g>`;
        return `<defs>${paints}<radialGradient id="me-vignette" cx="50%" cy="45%" r="75%"><stop offset="55%" stop-color="#000" stop-opacity="0"/><stop offset="100%" stop-color="#000" stop-opacity="0.55"/></radialGradient></defs>`
            + `<rect width="1200" height="675" fill="#05060a"/>${g('space', out.space)}`
            + `<g class="me-world__station">${g('subfloor', out.subfloor)}${g('cables', out.cables)}${g('gas pipes', out.pipes)}${g('turfs', out.turfs)}${g('wall mounts', out.mounts)}${g('areas', out.areas)}${g('scripts', out.scripts)}<g class="me-ghost"></g></g>`
            + '<rect width="1200" height="675" fill="url(#me-vignette)" pointer-events="none"/>';
    }

    // The ghost follows the tool: a floor fill for construct, walls round a floor for room, red cells for delete, an
    // outline on one object for select and dropper. The cursor badge (UI) gives a drag's size.
    function meRenderWorld(root) {
        const cv = root.parentElement; // the world and the cursor badge sit under the UI layer
        const svg = $('.me-world', cv);
        const station = $('.me-world__station', svg);
        const s = Math.pow(1.18, me.zoom);
        station.style.transform = `rotate(${me.rot * 90}deg) scale(${s})`;
        const lower = ME_MODES[me.mode].mode === 'lower' && me.tabs[me.mode] > 0; // cables, gas pipes, disposals hide turfs
        $$('[data-layer]', svg).forEach(gl => {
            const l = gl.dataset.layer;
            const on = l === 'space' || (l === 'turfs' ? me.layers.turfs && !lower : me.layers[l] !== false);
            gl.style.display = on ? '' : 'none';
        });
        svg.classList.toggle('me-world--atmos', me.view === 'atmos');
        svg.classList.toggle('me-world--power', me.view === 'power');
        const [gx, gy, gw, gh] = ME_GHOST;
        const cell = (c, r, kind) => {
            const fill = kind === 'del' ? 'rgb(235,98,82)' : '#fff';
            const fo = kind === 'wall' ? 0.34 : kind === 'del' ? 0.3 : 0.16;
            return `<rect x="${c * ME_T + 2}" y="${r * ME_T + 2}" width="${ME_T - 4}" height="${ME_T - 4}" rx="3" fill="${fill}" fill-opacity="${fo}" stroke="${fill}" stroke-opacity="0.75" stroke-width="1.5"/>`;
        };
        let ghost = '';
        let size = '';
        if (me.tool === 'construct' || me.tool === 'delete' || me.tool === 'room') {
            const rows = me.tool === 'room' ? gh : 2;
            for (let r = gy; r < gy + rows; r++) for (let c = gx; c < gx + gw; c++) {
                const border = r === gy || r === gy + rows - 1 || c === gx || c === gx + gw - 1;
                ghost += cell(c, r, me.tool === 'delete' ? 'del' : me.tool === 'room' && border ? 'wall' : 'floor');
            }
            size = `${gw} × ${rows}`;
        } else ghost = `<rect x="${13 * ME_T - 2}" y="${10 * ME_T - 2}" width="${ME_T + 4}" height="${ME_T + 4}" rx="4" fill="none" stroke="#fff" stroke-width="2"/>`;
        $('.me-ghost', svg).innerHTML = ghost;
        // The badge rides at the cursor, the drag's top-right corner here, through the camera's turn and zoom.
        const badge = $('.map-editor-cursor-badge', cv);
        badge.style.display = size ? '' : 'none';
        badge.textContent = size;
        const px = (gx + gw) * ME_T - 600;
        const py = gy * ME_T - 337.5;
        const a = me.rot * Math.PI / 2;
        badge.style.left = 600 + s * (px * Math.cos(a) - py * Math.sin(a)) + 4 + 'px';
        badge.style.top = 337.5 + s * (px * Math.sin(a) + py * Math.cos(a)) - 24 + 'px';
    }

    function meRender(root) {
        meRenderTools(root);
        meRenderLibrary(root);
        // The toast sits 12 px above the library, whatever its size.
        $('.map-editor-toast', root).style.bottom = 16 + $('.map-editor-bottom-row', root).offsetHeight + 12 + 'px';
        meRenderHint(root);
        meRenderCheck(root);
        meRenderWorld(root);
    }

    function buildMapEditor() {
        const cv = $('#me-canvas');
        if (!cv) return;
        const svg = document.createElementNS(SVG_NS, 'svg');
        svg.setAttribute('class', 'me-world');
        svg.setAttribute('viewBox', '0 0 1200 675');
        svg.innerHTML = meWorldSvg();
        cv.append(svg);
        cv.append(h('div', 'map-editor-cursor-badge'));

        const root = h('div', 'layer map-editor-root');
        const top = h('div', 'map-editor-region map-editor-region--top');
        ME_TOOLS.forEach(g => top.append(meStrip(g, 'down')));

        const right = h('div', 'map-editor-region map-editor-region--top-right');
        const col = h('div', 'map-editor-right-column');
        const dial = h('div');
        col.append(dial);
        ME_CAMERA_TOOLS.forEach(g => col.append(meStrip(g, 'left')));
        const views = h('div', 'map-editor-region map-editor-region--top-left');
        ME_VIEW_TOOLS.forEach(g => views.append(meStrip(g, 'right')));
        right.append(col);

        const bottom = h('div', 'map-editor-region map-editor-region--bottom map-editor-bottom-row');
        const rail = h('div', 'map-editor-mode-rail');
        ME_MODES.forEach((m, i) => {
            const t = h('div', 'menu-tab map-editor-mode-tab map-editor-mode-tab--' + m.mode, { text: m.mode });
            t.dataset.mode = i;
            rail.append(t);
        });
        const lib = h('div', 'storage-panel map-editor-library');
        const head = h('div', 'map-editor-library-header');
        const search = h('div', 'map-editor-search');
        search.append(h('div', 'map-editor-search-icon', { icon: meIcon('search') }));
        const input = h('input', 'map-editor-search-field');
        input.placeholder = 'search every tab';
        input.spellcheck = false;
        search.append(input);
        const btns = h('div', 'map-editor-library-header__buttons');
        [['min', 'chevronDown', 'minimize panel'], ['max', 'chevronUp', 'expand panel']].forEach(([k, icon, tip]) => {
            const b = h('div', 'map-editor-header-icon-btn');
            b.dataset.size = k;
            b.title = tip;
            b.append(h('div', 'map-editor-header-icon-btn__icon', { icon: meIcon(icon) }));
            btns.append(b);
        });
        head.append(h('div', 'map-editor-subcat-row'), search, btns);
        const scroll = h('div', 'map-editor-grid-scroll');
        scroll.append(h('div', 'map-editor-grid'));
        lib.append(head, scroll);
        bottom.append(rail, lib);

        root.append(h('div', 'map-editor-hint'), views, top, right, bottom, h('div', 'map-editor-toast'), h('div', 'me-popover-layer'));
        cv.append(root);
        buildCameraDial(dial);
        meRender(root);

        input.addEventListener('input', () => { me.search = input.value; meRenderLibrary(root); });
        input.addEventListener('focus', () => search.classList.add('map-editor-search--focus'));
        input.addEventListener('blur', () => search.classList.remove('map-editor-search--focus'));

        root.addEventListener('click', e => {
            const t = e.target;
            const tool = t.closest('[data-tool]');
            if (tool) {
                const id = tool.dataset.tool;
                const spec = [...ME_TOOLS, ...ME_VIEW_TOOLS, ...ME_CAMERA_TOOLS].flat().find(x => x.id === id);
                if (spec.pop) return meOpenPopover(root, spec.pop, tool);
                if (id === 'undo' || id === 'redo') {
                    const from = id === 'undo' ? 'undo' : 'redo';
                    if (!me[from]) return;
                    me[from]--;
                    me[from === 'undo' ? 'redo' : 'undo']++;
                    meToast(root, id === 'undo' ? 'undid: place grey floor × 14' : 'redid: place grey floor × 14');
                } else if (id === 'reset') {
                    me.rot = 0;
                    me.zoom = 0;
                } else if (id === 'atmos' || id === 'power') me.view = me.view === id ? null : id;
                else me.tool = id;
                return meRender(root);
            }
            const dialEl = t.closest('.map-editor-camera-dial');
            if (dialEl) {
                if (t.closest('.map-editor-camera-dial__open')) return meOpenPopover(root, 'camera', dialEl);
                const verb = dialEl.dataset.pointing;
                if (verb === 'zoom-in') me.zoom = Math.min(me.zoom + 1, 3);
                if (verb === 'zoom-out') me.zoom = Math.max(me.zoom - 1, -2);
                if (verb === 'rotate-left') me.rot--;
                if (verb === 'rotate-right') me.rot++;
                return meRenderWorld(root);
            }
            if (t.closest('[data-close]')) return meOpenPopover(root, me.pop);
            const sw = t.closest('.map-editor-switch');
            if (sw) {
                const on = !sw.classList.contains('map-editor-switch--on');
                sw.classList.toggle('map-editor-switch--on', on);
                if (sw.dataset.layer) me.layers[sw.dataset.layer] = on;
                if (sw.dataset.setting) me.settings[sw.dataset.setting] = on;
                return meRenderWorld(root);
            }
            const mapRow = t.closest('[data-map]');
            if (mapRow) {
                meToast(root, me.pop === 'save' ? `saved over ${mapRow.dataset.map}` : `loading ${mapRow.dataset.map}…`);
                return meOpenPopover(root, me.pop);
            }
            const popAct = t.closest('[data-pop-act]');
            if (popAct) {
                const name = $('.map-editor-popover input', root);
                meToast(root, popAct.dataset.popAct === 'save' ? `saved ${name ? name.value : 'map'}` : 'new map: an empty station');
                return meOpenPopover(root, me.pop);
            }
            const mode = t.closest('[data-mode]');
            if (mode) {
                me.mode = Number(mode.dataset.mode);
                me.search = input.value = '';
                return meRender(root);
            }
            const sub = t.closest('[data-subtab]');
            if (sub) {
                me.tabs[me.mode] = Number(sub.dataset.subtab);
                me.search = input.value = ''; // a tab click clears the search
                return meRender(root);
            }
            const size = t.closest('[data-size]');
            if (size) {
                me.size = me.size === size.dataset.size ? 'normal' : size.dataset.size;
                return meRender(root);
            }
            const slot = t.closest('[data-entry]');
            if (slot) {
                const key = slot.dataset.entry;
                const cyc = t.closest('[data-cycle]');
                const { m, t: ti, e: entry } = meEntry(key);
                if (cyc) me.variants[key] = ((me.variants[key] || 0) + Number(cyc.dataset.cycle) + entry.variants.length) % entry.variants.length;
                me.picked = key;
                // A pick opens the entry's own tab, and from select, dropper or delete switches to construct.
                me.mode = m;
                me.tabs[m] = ti;
                if (!ME_TOOL_TABS[me.tool]) me.tool = 'construct';
                return meRender(root);
            }
            const issue = t.closest('[data-issue]');
            const act = t.closest('[data-act]');
            if (act && issue) {
                const i = Number(issue.dataset.issue);
                if (act.dataset.act === 'ignore') me.check.ignored.add(i);
                if (act.dataset.act === 'show in palette') {
                    me.mode = 3;
                    me.tabs[3] = 0;
                    me.picked = '3/0/0';
                }
                if (act.dataset.act === 'locate') meToast(root, 'camera moved to (14, 9)');
                return meRender(root);
            }
            if (issue) {
                me.check.open = me.check.open === Number(issue.dataset.issue) ? -1 : Number(issue.dataset.issue);
                return meRenderCheck(root);
            }
            const filter = t.closest('[data-filter]');
            if (filter) {
                me.check.filter = filter.dataset.filter;
                return meRenderCheck(root);
            }
        });

        // The editor's hotkeys while the pointer is over it: 1-5 pick a tool, V / Shift+V cycle the held family.
        let over = false;
        cv.addEventListener('pointerenter', () => { over = true; });
        cv.addEventListener('pointerleave', () => { over = false; });
        document.addEventListener('keydown', e => {
            if (!over || e.target.closest('input') || e.ctrlKey || e.metaKey || e.altKey) return;
            const n = Number(e.key);
            if (n >= 1 && n <= 5) {
                me.tool = ME_TOOLS[0][n - 1].id;
                meRender(root);
            } else if (e.key.toLowerCase() === 'v') {
                const { e: entry } = meEntry(me.picked);
                if (!entry.variants) return;
                me.variants[me.picked] = ((me.variants[me.picked] || 0) + (e.shiftKey ? -1 : 1) + entry.variants.length) % entry.variants.length;
                meRender(root);
            } else if (e.key === 'Escape' && me.pop) meOpenPopover(root, me.pop);
        });
    }

    // ══════════════════════════════════════════════════════════════════════
    // States: every element in each of its states
    // ══════════════════════════════════════════════════════════════════════
    // Hover, pressed and focus are pseudo-classes. To show them side by side, each :hover / :active / :focus rule in
    // components.css gets a twin right after it with .is-hover / .is-pressed / .is-focus instead: same specificity, same
    // place in the cascade, so a forced state draws exactly like the real one.
    function addForcedStates() {
        const sheet = [...document.styleSheets].find(s => s.href && s.href.endsWith('/components.css'));
        if (!sheet) return;
        const rx = /:hover|:active|:focus(?![-\w])/;
        const swap = s => s.replace(/:hover/g, '.is-hover').replace(/:active/g, '.is-pressed').replace(/:focus(?![-\w])/g, '.is-focus');
        for (let i = sheet.cssRules.length - 1; i >= 0; i--) {
            const r = sheet.cssRules[i];
            if (!(r instanceof CSSStyleRule) || !rx.test(r.selectorText)) continue;
            const sel = r.selectorText.split(',').map(s => s.trim()).filter(s => rx.test(s)).map(swap).join(', ');
            sheet.insertRule(`${sel} { ${r.style.cssText} }`, i + 1);
        }
    }

    // One row per element: [name, markup, the element in it that takes the state, { state: classes, or attributes }].
    // The markup goes through the normal builders (data-slot, data-build, data-icon), so a state is the real component.
    const HOVER = { rest: '', hover: 'is-hover' };
    const PRESS = { idle: '', hover: 'is-hover', pressed: 'is-hover is-pressed' };
    const STATES = {
        hud: [
            ['gear slot', '<div data-slot="UI/Card|glyph"></div>', '[data-slot]', HOVER],
            ['hand', '<div data-slot="Rendered/Radio|wide|L"></div>', '[data-slot]',
                { rest: '', hover: 'is-hover', active: 'inventory-slot--active', 'active, hover': 'inventory-slot--active is-hover' }],
            ['equipment frame', '<div class="equipment-grid"><div data-slot="Inventory/BeepHead|frame"></div></div>', '[data-slot]', HOVER],
            ['equipment pocket', '<div class="equipment-grid"><div data-slot="Inventory/BeepPocket|frameless|frame"></div></div>', '[data-slot]', HOVER],
            ['sheet frame', '<div class="character-paperdoll state-sheet"><div><div data-slot="Inventory/Waist|frame|sm"></div></div></div>', '[data-slot]', HOVER],
            ['storage cell', '<div class="storage-panel"><div class="storage-slot"><div class="storage-slot__icon" data-icon="Rendered/CD"></div></div></div>', '.storage-slot', HOVER],
            ['pile cell', '<div class="pile-picker"><div class="pile-picker__cell"><div class="pile-picker__shadow" data-icon="Rendered/CD"></div><div class="pile-picker__icon" data-icon="Rendered/CD"></div></div></div>', '.pile-picker__cell', HOVER],
            ['radial', '<div class="state-radial"><div data-build="radial"></div></div>', '[data-build]',
                Object.fromEntries(RADIAL.map(o => [o.label, { 'data-point': o.label }]))],
        ],
        menu: [
            ['tab', '<div class="menu-tabs"><div class="menu-tab">tab</div></div>', '.menu-tab',
                { idle: '', hover: 'is-hover', selected: 'menu-tab--selected', disabled: 'menu-tab--disabled' }],
            ['segmented option', '<div class="menu-segmented"><div class="menu-segmented__option">option</div></div>', '.menu-segmented__option',
                { idle: '', hover: 'is-hover', selected: 'menu-segmented__option--selected' }],
            ['text field', '<div class="state-field"><input class="menu-text-field" placeholder="Enter name" spellcheck="false"></div>', 'input',
                { empty: '', focus: 'is-focus', filled: { value: 'Ezra Hale' } }],
            ['dropdown', '<div class="state-field"><div class="menu-dropdown"><div class="menu-dropdown__value">value</div><div class="menu-dropdown__arrow"></div><div class="menu-dropdown__list"><div class="menu-dropdown__item">value</div><div class="menu-dropdown__item">another</div><div class="menu-dropdown__item">third</div></div></div></div>', '.menu-dropdown',
                { closed: '', hover: 'is-hover', open: 'menu-dropdown--open' }],
            ['slider', '<div class="state-field"><div class="menu-slider" data-value="0.6"><div class="menu-slider__tracker"></div><div class="menu-slider__fill"></div><div class="menu-slider__dragger"></div></div></div>', '.menu-slider',
                { idle: '', hover: 'is-hover', dragging: 'menu-slider--dragging' }],
            ['button', '<div class="menu-button"><div class="menu-button__tip"></div><div class="menu-button__text">continue</div></div>', '.menu-button', PRESS],
            ['secondary button', '<div class="menu-button menu-button--secondary"><div class="menu-button__tip"></div><div class="menu-button__text">back</div></div>', '.menu-button', PRESS],
            ['style tile', '<div class="style-tile"></div>', '.style-tile', { idle: '', hover: 'is-hover', selected: 'style-tile--selected' }],
        ],
        editor: [
            ['tool', '<div class="storage-panel"><div class="storage-slot map-editor-icon-btn"><div class="map-editor-icon-btn__icon" data-icon="MapEditor/construct.svg"></div><div class="map-editor-icon-btn__key">1</div></div></div>', '.map-editor-icon-btn',
                { rest: '', hover: 'is-hover', active: 'map-editor-icon-btn--active', disabled: 'map-editor-icon-btn--disabled' }],
            ['palette slot', '<div class="storage-panel state-pad"><div class="map-editor-slot"><div class="map-editor-slot__icon-frame"><div class="me-thumb me-thumb--floor" style="--c:#8b8e92"></div></div><div class="map-editor-slot__label">grey</div></div></div>', '.map-editor-slot',
                { rest: '', hover: 'is-hover', picked: 'map-editor-slot--active' }],
            ['slot, no render', '<div class="storage-panel state-pad"><div class="map-editor-slot"><div class="map-editor-slot__icon-frame"><div class="map-editor-slot__icon map-editor-slot__icon--glyph" data-icon="MapEditor/settings.svg"></div></div><div class="map-editor-slot__label">smes</div></div></div>', '.map-editor-slot',
                { rest: '', hover: 'is-hover', picked: 'map-editor-slot--active' }],
            ['family slot', '<div class="storage-panel state-pad"><div class="map-editor-slot map-editor-slot--family"><div class="map-editor-slot__icon-frame"><div class="me-thumb me-thumb--door" style="--c:#34e2ec"></div><div class="map-editor-slot__variant">standard</div><div class="map-editor-slot__badge">1/2</div><div class="map-editor-slot__cycle map-editor-slot__cycle--previous">‹</div><div class="map-editor-slot__cycle map-editor-slot__cycle--next">›</div></div><div class="map-editor-slot__label">medical airlock</div></div></div>', '.map-editor-slot',
                { rest: '', hover: 'is-hover', picked: 'map-editor-slot--active' }],
            ['sub-tab', '<div class="storage-panel state-pad"><div class="map-editor-subcat-row"><div class="map-editor-subcat-tab">floors</div></div></div>', '.map-editor-subcat-tab',
                { idle: '', hover: 'is-hover', selected: 'map-editor-subcat-tab--active' }],
            ['mode tab', '<div class="map-editor-mode-rail"><div class="menu-tab map-editor-mode-tab">upper</div></div>', '.menu-tab',
                { idle: '', hover: 'is-hover', selected: 'menu-tab--selected' }],
            ['switch', '<div class="map-editor-switch"><div class="map-editor-switch__thumb"></div></div>', '.map-editor-switch',
                { off: '', hover: 'is-hover', on: 'map-editor-switch--on' }],
            ['camera dial', '<div class="state-dial"><div data-build="camera-dial"></div></div>', '[data-build]',
                { rest: '', ...Object.fromEntries(DIAL.map(v => [v.label, { 'data-point': v.label }])) }],
        ],
    };

    // Runs before the builders, so each copy is then built like any other placeholder on the page.
    function buildStates() {
        $$('[data-states]').forEach(host => {
            (STATES[host.dataset.states] || []).forEach(([name, markup, target, states]) => {
                const row = h('div', 'state-row');
                row.append(h('div', 'state-row__name', { text: name }));
                Object.entries(states).forEach(([label, set]) => {
                    const item = h('div', 'demo-item');
                    item.innerHTML = markup;
                    const t = $(target, item);
                    if (typeof set === 'string') set.split(' ').filter(Boolean).forEach(c => t.classList.add(c));
                    else Object.entries(set).forEach(([k, v]) => t.setAttribute(k, v));
                    item.append(h('div', 'cap', { text: label }));
                    row.append(item);
                });
                host.append(row);
            });
        });
    }

    // ══════════════════════════════════════════════════════════════════════
    // Tabs + boot
    // ══════════════════════════════════════════════════════════════════════
    // Each nav link is a tab showing one section. The hash names the tab, so links, reloads and back/forward work;
    // with no hash the last tab viewed comes back.
    function wireTabs() {
        const tabs = $$('.nav__tabs a[href^="#"]');
        const ids = tabs.map(a => a.getAttribute('href').slice(1));
        const panels = new Map(ids.map(id => [id, document.getElementById(id)]));
        let current = null;

        tabs.forEach((a, i) => {
            a.id = 'tab-' + ids[i];
            a.setAttribute('role', 'tab');
            a.setAttribute('aria-controls', ids[i]);
            const p = panels.get(ids[i]);
            p.setAttribute('role', 'tabpanel');
            p.setAttribute('aria-labelledby', a.id);
            // Previous / next at the bottom, so the tabs can be stepped through like the old long page.
            const pager = document.createElement('nav');
            pager.className = 'pager';
            pager.setAttribute('aria-label', 'More sections');
            const link = (j, cls, dir) => {
                if (j < 0 || j >= ids.length) return;
                const l = document.createElement('a');
                l.className = cls;
                l.href = '#' + ids[j];
                const tag = document.createElement('span');
                tag.textContent = dir;
                l.append(tag, tabs[j].textContent);
                pager.append(l);
            };
            link(i - 1, 'pager__prev', '← previous');
            link(i + 1, 'pager__next', 'next →');
            p.append(pager);
        });

        // Scroll only the tab row, never the page: scrollIntoView would also drag the window sideways when a specimen
        // is wider than the screen.
        const row = $('.nav__tabs');
        function keepInView(a) {
            const r = a.getBoundingClientRect();
            const b = row.getBoundingClientRect();
            if (r.left < b.left || r.right > b.right) row.scrollLeft += r.left - b.left - (b.width - r.width) / 2;
        }

        function show(id, how) {
            if (!panels.has(id)) id = ids[0];
            if (id === current) return;
            current = id;
            panels.forEach((p, k) => p.classList.toggle('is-active', k === id));
            tabs.forEach(a => {
                const on = a.getAttribute('href') === '#' + id;
                a.classList.toggle('is-current', on);
                a.setAttribute('aria-selected', on);
                a.tabIndex = on ? 0 : -1;
                if (on) keepInView(a);
            });
            store.set('tab', id);
            if (how === 'push') history.pushState(null, '', '#' + id);
            else if (how === 'replace') history.replaceState(null, '', '#' + id);
            if (how !== 'boot') window.scrollTo({ top: 0, behavior: 'instant' });
            reanchor();
        }

        // Any in-page link to a tab (nav, pager, notes) switches tabs instead of scrolling.
        document.addEventListener('click', e => {
            const a = e.target.closest('a[href^="#"]');
            if (!a || e.button || e.ctrlKey || e.metaKey || e.shiftKey || e.altKey) return;
            const id = a.getAttribute('href').slice(1);
            if (!panels.has(id)) return;
            e.preventDefault();
            show(id, 'push');
        });
        window.addEventListener('popstate', () => show(location.hash.slice(1)));
        window.addEventListener('hashchange', () => show(location.hash.slice(1)));

        // Left and right arrows move along the tab row.
        $('.nav__tabs').addEventListener('keydown', e => {
            const i = ids.indexOf(current);
            const step = { ArrowRight: 1, ArrowLeft: -1 }[e.key];
            let j = step ? i + step : e.key === 'Home' ? 0 : e.key === 'End' ? ids.length - 1 : -1;
            if (j < 0 && !step) return;
            j = (j + ids.length) % ids.length;
            e.preventDefault();
            show(ids[j], 'replace');
            tabs[j].focus();
        });

        document.documentElement.dataset.tabbed = '';
        const start = location.hash.slice(1);
        show(panels.has(start) ? start : store.get('tab'), 'boot');
    }

    function boot() {
        if (location.protocol === 'file:') $('#file-banner').classList.add('is-shown');
        const errBanner = $('#boot-error');
        $('[data-reset-storage]', errBanner).addEventListener('click', () => {
            Object.keys(localStorage).filter(k => k.startsWith('ss3d-ui-bible:')).forEach(k => localStorage.removeItem(k));
            location.reload();
        });
        // Each step runs on its own: one that throws is reported in the banner, and the rest still start.
        const step = (name, fn) => {
            try {
                fn();
            } catch (e) {
                console.error(e);
                errBanner.classList.add('is-shown');
                $('[data-errors]', errBanner).textContent += `${name}: ${e.message}\n`;
            }
        };

        step('states', buildStates);
        step('storage panels', () => $$('[data-storage]').forEach(buildStorage));
        step('equipment grid', () => $$('[data-build="equipment"]').forEach(buildEquipment));
        step('search sheet', () => $$('[data-build="paperdoll"]').forEach(buildPaperdoll));
        step('pile picker', () => $$('[data-build="pile"]').forEach(buildPile));
        step('radial', () => $$('[data-build="radial"]').forEach(buildRadial));
        step('map editor', () => {
            $$('[data-build="camera-dial"]').forEach(buildCameraDial);
            buildMapEditor();
        });
        step('slots', expandSlots);
        step('icons', () => $$('[data-icon]').forEach(el => setIcon(el, el.dataset.icon)));
        step('hair styles', buildStyles);

        step('swatches', buildSwatches);
        step('sizes', buildSizes);
        step('icon list', buildIcons);

        step('forced states', addForcedStates);

        step('specimen tools', wireSpecs);
        step('reference compare', wireCompare);
        step('stages', wireStages);
        step('hands', wireHands);
        step('menus', wireMenus);
        step('sliders', wireSliders);
        step('colour picker', wireColorPicker);
        step('containers', () => anchorRoots().forEach(wireContainers));
        step('tabs', wireTabs);

        const nameField = $('#cs-canvas [data-bind="name"]');
        if (nameField) nameField.addEventListener('input', updateCode);

        step('layout', reanchor);
        window.addEventListener('resize', reanchor);
        if (document.fonts && document.fonts.ready) document.fonts.ready.then(reanchor);
    }

    boot();
})();
