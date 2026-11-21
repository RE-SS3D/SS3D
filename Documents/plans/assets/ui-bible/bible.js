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
        Wallpapers: 'Assets/Art/Textures/UI/Wallpapers/',
        Logos: 'Assets/Art/Textures/UI/Logos/',
        Renders: 'Assets/Art/Textures/UI/Renders/',
        Banners: 'Assets/Art/Textures/Lobby/Banners/',
        Styles: 'Assets/Art/Icons/Lobby/Styles/',
        Jobs: 'Assets/Art/Icons/Lobby/Jobs/',
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
        ['State and news (new)', [['--ui-info', 'news: an update, a changed setting, a countdown, LOOC']]],
        ['Departments (lobby)', ['command', 'security', 'engineering', 'medical', 'science', 'cargo', 'service', 'civilian'].map(d => ['--ui-dept-' + d, d])],
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
        // The shipped screens ("old"), captured in the Editor at 1920 × 1080: the main menu from MainMenuPreviewWindow's
        // sample states (at 1600 × 900), the lobby from a local host session. The shell panel is 1920 px wide there
        // (constant pixel size), so each capture spans the whole stage and the old UI keeps its true share of the screen.
        // The preview leaves Settings' panel empty (it needs the running game), so Settings shows the same panel as
        // shipped in the lobby.
        mainmenu: ['home', 'first-launch', 'no-name', 'update', 'join', 'details', 'refused', 'settings', 'connecting',
            'failed', 'connection-lost', 'hosting', 'updating']
            .map(n => (n === 'settings'
                ? { img: 'old/lobby-settings', tab: 'settings (lobby)', scale: 1920 / 1200, blur: 0.5, at: [0, 0], to: '.main-menu', old: true }
                : { img: 'old/main-menu-' + n, tab: n.replace(/-/g, ' '), scale: 1600 / 1200, blur: 0.5, at: [0, 0], to: '.main-menu', old: true })),
        // The shipped character window, captured in the Editor at 1920 × 1080 (one per tab).
        charwin: ['appearance', 'jobs', 'antags']
            .map(n => ({ img: 'old/charwin-' + n, tab: n, scale: 1920 / 1200, blur: 0.5, at: [0, 0], to: '.cw', old: true })),
        lobby: ['pre-round', 'ready', 'in-progress', 'join', 'switcher', 'chat', 'settings', 'round-over', 'server-settings']
            .map(n => ({ img: 'old/lobby-' + n, tab: n.replace(/-/g, ' '), scale: 1920 / 1200, blur: 0.5, at: [0, 0], to: '.lobby', old: true })),
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
            const old = refs[0].old;
            bar.append(h('span', '', { text: old ? 'design' : 'reference' }));
            const modes = h('div', 'cmp-bar__group');
            const modeList = old ? [['ui', 'new'], ['old', 'old'], ['side', 'side by side'], ['overlay', 'overlay']]
                : [['ui', 'ui only'], ['side', 'side by side'], ['overlay', 'overlay']];
            modeList.forEach(([m, text]) => {
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
    // Main menu: MainMenuView's tree (and PlayerSettingsPanel's) in the map editor's look
    // ══════════════════════════════════════════════════════════════════════
    // The menu's only wallpaper (owner, 2026-11-21): no random pick, rotation or video.
    const MM_WALLPAPER = 'Clownin';
    const MM_TRACKS = ['Breach', 'Gateway to the Forest', 'Space Theme', "Spaceman's Lullaby", 'Title 1', 'Title 2', 'Title 3'];
    // Sample servers. The addresses are documentation ranges; the LAN one is main-menu.md's example C.
    const MM_SERVERS = [
        { id: 'sandbox', name: 'RE:SS3D Sandbox', address: 'sandbox.ss3d.space:1151', state: 'ok', players: '12/32', ping: '38 ms' },
        { id: 'night', name: 'Night shift', address: '203.0.113.24:1151', state: 'full', players: '16/16', ping: '64 ms' },
        { id: 'friday', name: 'Friday build test', address: '198.51.100.7:7777', state: 'update', players: '3/16', ping: '112 ms' },
        { id: 'lan', name: 'LAN party', address: '192.168.1.20:1151', state: 'offline', players: '—', ping: '—' },
    ];
    const MM_STATUS = {
        ok: ['Online', 'ok', ''],
        full: ['Full', 'warn', 'warn'],
        update: ['Needs v0.5.0 — update', 'info', ''],
        offline: ['Offline', 'danger', 'danger'],
    };
    // One chip per state MainMenuView can show (MainMenuPreviewWindow's sample states, roughly).
    const MM_STATES = {
        'home': {},
        'first launch': { first: true },
        'no name': { name: '' },
        'update': { update: true },
        'join': { screen: 'servers' },
        'details': { screen: 'servers', expanded: 'sandbox' },
        'refused': { screen: 'servers', refused: true },
        'settings': { screen: 'settings' },
        'connecting': { overlay: 'connect', step: 1 },
        'failed': { overlay: 'connect', step: 1, failed: true },
        'connection lost': { overlay: 'connect', lost: true },
        'hosting': { overlay: 'host', step: 0 },
        'updating': { overlay: 'update', step: 0 },
    };
    const MM_STEPS = {
        connect: ['Contacting server', 'Checking version', 'Loading world', 'Logging in'],
        host: ['Starting host', 'Loading world', 'Opening lobby'],
        update: ['Downloading update', 'Keeping maps and settings', 'Restarting'],
    };
    const MM_OFF_ON = ['Off', 'On'];
    const MM_SETTINGS = {
        display: {
            intro: 'Changes apply right away. Resolution and window mode ask before they stick, and revert after 15 s if not kept.',
            groups: [
                ['Screen', [
                    { key: 'resolution', label: 'Resolution', select: ['2560 × 1440', '1920 × 1080', '1600 × 900', '1280 × 720'] },
                    { key: 'windowMode', label: 'Window mode', seg: ['Fullscreen', 'Borderless', 'Windowed'] },
                    { key: 'vsync', label: 'VSync', seg: MM_OFF_ON },
                    { key: 'frameCap', label: 'Frame cap', seg: ['Off', '30', '60', '120', '144', '240'],
                        locked: s => s.vsync === 1, note: s => (s.vsync === 1 ? 'Not used while VSync is on.' : '') },
                ]],
                ['Rendering', [
                    { key: 'quality', label: 'Quality preset', seg: ['Low', 'Medium', 'High', 'Ultra'],
                        note: () => 'Shadow distance, shadow resolution and similar.' },
                    { key: 'renderScale', label: 'Render scale', seg: ['100 %', '77 %', '67 %', '50 %'] },
                    { key: 'upscaler', label: 'Upscaler', seg: ['STP', 'FSR 1', 'Bilinear'],
                        note: s => (s.renderScale === 0 ? 'Only upscales below 100 % render scale.' : '') },
                    { key: 'aa', label: 'Anti-aliasing', seg: ['Off', 'FXAA', 'SMAA', 'TAA'] },
                ]],
                ['Interface', [
                    { key: 'uiScale', label: 'UI scale', slider: [75, 150, ' %'], note: () => 'Applies as you drag.' },
                    { key: 'showFps', label: 'Show FPS counter', seg: MM_OFF_ON },
                    { key: 'showLog', label: 'Show log indicator', seg: MM_OFF_ON,
                        note: () => 'On-screen error and warning count, for bug reports.' },
                ]],
            ],
        },
        audio: {
            intro: 'Changes apply right away.',
            groups: [
                ['Volume', ['Master', 'Music', 'SFX', 'Ambience', 'Personal'].map(l => ({ key: 'vol' + l, label: l, slider: [0, 100, ' %'] }))],
                ['Muting', [
                    { key: 'muteAll', label: 'Mute all', seg: MM_OFF_ON, note: () => 'Silences output. Levels stay as set.' },
                    { key: 'muteUnfocused', label: 'Mute when window unfocused', seg: MM_OFF_ON },
                ]],
                ['Main menu', [
                    { key: 'menuMusic', label: 'Menu music', seg: MM_OFF_ON, note: () => 'Plays on the main menu and in the lobby.' },
                ]],
            ],
        },
        controls: {
            intro: 'Click an action, then press a key or mouse button 3–5. Esc or a second click cancels. Taking a key another action uses swaps the two.',
            binds: [
                ['Movement', [['Move up', 'W'], ['Move left', 'A'], ['Move down', 'S'], ['Move right', 'D'], ['Run', 'Shift']]],
                ['Hands', [['Swap hands', 'X'], ['Drop', 'Q'], ['Throw', 'R'], ['Use in hand', 'Z']]],
                ['Communication', [['Talk', 'T'], ['Radio', 'Y'], ['Emote', 'M']]],
                ['Map Editor', [['Open the editor', 'B'], ['Rotate', 'R'], ['Undo', 'Ctrl + Z']]],
            ],
        },
    };
    const MM_SETTING_DEFAULTS = {
        resolution: '1920 × 1080', windowMode: 1, vsync: 1, frameCap: 0, quality: 2, renderScale: 0, upscaler: 0, aa: 3,
        uiScale: 100, showFps: 0, showLog: 0, volMaster: 80, volMusic: 50, volSFX: 80, volAmbience: 70, volPersonal: 80,
        muteAll: 0, muteUnfocused: 0, menuMusic: 1,
    };
    const MM_BASE = {
        screen: 'home', overlay: null, step: 0, failed: false, lost: false, first: false, update: false, refused: false,
        expanded: null, name: 'Spessman', port: '1151', advanced: false, address: '', music: true, track: 0, volume: 0.5,
        tab: 'display', listening: null,
    };
    const mm = { ...MM_BASE, chip: 'home', settings: { ...MM_SETTING_DEFAULTS, windowMode: 0, quality: 3 } };
    let mmTimer = 0;

    function mmText(parent, text, cls) {
        const e = h('div', 'main-menu__text' + (cls ? ' ' + cls : ''), { text });
        parent.append(e);
        return e;
    }

    function mmEl(parent, cls) {
        const e = h('div', cls);
        parent.append(e);
        return e;
    }

    // A white glyph tinted by its CSS background colour (web-only mask; USS: background-image and
    // -unity-background-image-tint-color).
    function mmGlyph(parent, cls, key) {
        const e = mmEl(parent, cls);
        const u = cssUrl('Heroicons/Outline/' + key);
        e.style.webkitMaskImage = u;
        e.style.maskImage = u;
        return e;
    }

    const MM_NAV_ICONS = { join: 'GlobeAlt', local: 'Server', settings: 'Cog', quit: 'Logout' };
    // A server's state as its icon (main-menu__state-icon--*); the Checking one spins in game.
    const MM_STATE_ICONS = { ok: ['online', 'StatusOnline'], full: ['full', 'UserGroup'], update: ['update', 'CloudDownload'],
        offline: ['offline', 'StatusOffline'] };
    // The dialog's kind sets its colour (--op): hosting ok, joining info, updating warn, lost or failed danger.
    const MM_OP_ICONS = { host: 'Server', join: 'GlobeAlt', update: 'CloudDownload', lost: 'StatusOffline', failed: 'Exclamation' };
    const MM_STEP_ICONS = { active: 'Refresh', done: 'Check', failed: 'X' };
    let mmOverlayShown = false;
    let mmLastShare = 0;

    function mmButton(parent, text, cls, action, arg) {
        const b = mmText(parent, text, 'main-menu__button' + (cls ? ' ' + cls : ''));
        if (action) b.dataset.mm = action;
        if (arg) b.dataset.arg = arg;
        return b;
    }

    function mmInput(parent, field, value, cls, placeholder, password) {
        const i = h('input', 'main-menu__input' + (cls ? ' ' + cls : ''));
        i.value = value;
        i.spellcheck = false;
        i.dataset.field = field;
        if (placeholder) i.placeholder = placeholder;
        if (password) i.type = 'password';
        parent.append(i);
        return i;
    }

    function mmSlider(parent, value, cls, key) {
        const s = h('div', 'menu-slider ' + cls);
        s.append(h('div', 'menu-slider__tracker'), h('div', 'menu-slider__fill'), h('div', 'menu-slider__dragger'));
        s.dataset.mmSlider = key;
        parent.append(s);
        setSlider(s, value);
        return s;
    }

    function mmSidebar(root) {
        const hasName = mm.name.trim() !== '';
        const side = mmEl(root, 'main-menu__sidebar');
        side.id = 'sidebar';
        const logo = mmEl(side, 'main-menu__logo');
        logo.style.backgroundImage = cssUrl('Logos/SS3DLogo');

        if (mm.update) {
            const up = mmEl(side, 'main-menu__update');
            const head = mmEl(up, 'main-menu__row');
            mmEl(head, 'main-menu__dot main-menu__dot--info');
            mmText(head, 'New version available', 'main-menu__update-title');
            mmText(up, 'v0.5.0 is out; this is v0.4.2. 1 server in your list needs it.', 'main-menu__banner-copy');
            const row = mmEl(up, 'main-menu__row');
            mmButton(row, 'Update and restart', 'main-menu__button--accent', 'overlay', 'update');
            mmText(row, 'Keeps maps and settings', 'main-menu__hint main-menu__beside');
        }

        const id = mmEl(side, 'main-menu__identity');
        mmText(id, 'Name', 'main-menu__eyebrow');
        mmInput(id, 'name', mm.name, '', 'Your name');
        if (!hasName) mmText(id, 'Enter a name to join or play.', 'main-menu__hint main-menu__hint--warning');
        else if (mm.first) mmText(id, "Taken from this computer's user name. Change it any time.", 'main-menu__hint');

        const nav = mmEl(side, 'main-menu__navigation');
        let joinSub = 'Recent, public or direct connect';
        if (!hasName) joinSub = 'Needs a name';
        else if (mm.first) joinSub = 'Paste an address from Discord';
        else joinSub = 'Last played: ' + MM_SERVERS[0].name;
        const selected = mm.screen === 'servers' ? 'join' : mm.screen === 'settings' ? 'settings' : null;
        const row = (parent, name, title, sub, extra) => {
            const r = mmEl(parent, 'main-menu__nav main-menu__nav--' + name + (extra ? ' ' + extra : ''));
            r.dataset.mm = name;
            mmGlyph(r, 'main-menu__nav-icon', MM_NAV_ICONS[name]);
            if (sub == null) {
                r.classList.add('main-menu__title');
                mmText(r, title, '');
            } else {
                const text = mmEl(r, 'main-menu__nav-text');
                mmText(text, title, 'main-menu__title');
                mmText(text, sub, 'main-menu__nav-subtitle');
            }
            if (selected === name) r.classList.add('main-menu__nav--selected');
            r.append(h('div', 'main-menu__nav-tip'));
            return r;
        };
        const join = row(nav, 'join', 'Join a server', joinSub, selected ? '' : 'main-menu__button--accent');
        const localRow = mmEl(nav, 'main-menu__local-row');
        const local = row(localRow, 'local', 'Play locally', hasName ? `Host on this computer, port ${mm.port}` : 'Needs a name');
        if (!hasName) [join, local].forEach(r => r.classList.add('main-menu__nav--disabled'));
        const adv = mmEl(localRow, 'main-menu__advanced');
        adv.dataset.mm = 'advanced';
        mmText(adv, 'Port', 'main-menu__advanced-label');
        mmEl(adv, 'main-menu__caret' + (mm.advanced ? ' main-menu__caret--up' : ''));
        if (mm.advanced) {
            const port = mmEl(nav, 'main-menu__port');
            mmText(port, 'Port', 'main-menu__eyebrow');
            mmInput(port, 'port', mm.port, 'main-menu__port-input');
            mmText(port, 'Friends on your network join on this port.', 'main-menu__hint');
        }
        row(nav, 'settings', 'Settings');
        row(nav, 'quit', 'Quit');

        mmEl(side, 'main-menu__spacer');
        const music = mmEl(side, 'main-menu__music' + (mm.music ? '' : ' main-menu__music--off'));
        const toggle = mmEl(music, 'main-menu__music-toggle' + (mm.music ? ' main-menu__music-toggle--on' : ''));
        toggle.dataset.mm = 'music';
        toggle.title = mm.music ? 'Turn music off' : 'Turn music on';
        toggle.append(h('div', 'main-menu__music-icon', { icon: 'Heroicons/Solid/MusicNote' }));
        const text = mmEl(music, 'main-menu__music-text');
        mmText(text, 'Music', 'main-menu__music-eyebrow');
        mmText(text, mm.music ? MM_TRACKS[mm.track] : 'Off', 'main-menu__music-title');
        const next = mmEl(music, 'main-menu__music-next');
        next.dataset.mm = 'next';
        next.title = 'Next track';
        next.append(h('div', 'main-menu__music-icon', { icon: 'Heroicons/Solid/FastForward' }));
        mmSlider(music, mm.volume, 'main-menu__music-volume', 'volume');

        const foot = mmEl(side, 'main-menu__footer');
        const a = mmEl(foot, 'main-menu__row main-menu__spread');
        mmText(a, 'v0.4.2 pre-alpha', 'main-menu__fine');
        mmText(a, 'Discord', 'main-menu__link').dataset.mm = 'discord';
        const b = mmEl(foot, 'main-menu__row');
        mmText(b, 'Wallpapers:', 'main-menu__fine');
        mmText(b, 'RE:SS3D art contributors', 'main-menu__link').dataset.mm = 'credits';
    }

    function mmServer(parent, s, primary) {
        const hasName = mm.name.trim() !== '';
        const [status, , tone] = MM_STATUS[s.state];
        const card = mmEl(parent, 'main-menu__server');
        const row = mmEl(card, 'main-menu__server-row');
        const [stateIcon, stateKey] = MM_STATE_ICONS[s.state];
        mmGlyph(row, 'main-menu__state-icon main-menu__state-icon--' + stateIcon, stateKey);
        const id = mmEl(row, 'main-menu__server-identity');
        mmText(id, s.name, 'main-menu__server-name');
        mmText(id, s.address, 'main-menu__server-address');
        mmText(row, status, 'main-menu__status' + (tone ? ' main-menu__status--' + tone : ''));
        mmText(row, s.players, 'main-menu__players' + (s.state === 'full' ? ' main-menu__players--warn' : ''));
        mmText(row, s.ping, 'main-menu__ping');
        const actions = mmEl(row, 'main-menu__actions');
        const open = mm.expanded === s.id;
        mmText(actions, open ? 'Hide' : 'Details', 'main-menu__details-button').dataset.mm = 'details';
        actions.lastChild.dataset.arg = s.id;
        const update = s.state === 'update';
        let cls = update ? 'main-menu__button--blue' : primary ? 'main-menu__button--accent' : '';
        if (!hasName || s.state === 'full' || s.state === 'offline') cls += ' main-menu__button--disabled';
        mmButton(actions, update ? 'Update' : 'Join', cls, update ? 'overlay' : 'join-server', update ? 'update' : s.id);
        if (mm.refused && s.state === 'full') {
            const r = mmEl(card, 'main-menu__refusal');
            mmText(r, 'The server is full. You can try again when a slot opens.', '');
            mmText(r, '2 min ago', 'main-menu__fine');
        }
        if (open) {
            const d = mmEl(card, 'main-menu__details');
            const line = key => {
                const l = mmEl(d, 'main-menu__detail-row');
                mmText(l, key, 'main-menu__detail-key');
                return l;
            };
            mmInput(line('Name'), 'server-name', s.name, 'main-menu__detail-value');
            mmText(line('Address'), s.address, 'main-menu__detail-text');
            const tok = line('Admin token');
            mmInput(tok, 'token', '', 'main-menu__detail-value', 'Optional', true);
            mmText(tok, 'Show', 'main-menu__details-button').dataset.mm = 'show-token';
            const foot = mmEl(d, 'main-menu__details-footer');
            mmText(foot, 'Saved automatically. The token is sent when joining; admins join like everyone else.', 'main-menu__hint');
            mmButton(foot, 'Remove', 'main-menu__button--danger', 'toast', 'In game: the server leaves your recent list');
        }
    }

    function mmServers(panel) {
        const hasName = mm.name.trim() !== '';
        const head = mmEl(panel, 'main-menu__header');
        mmText(head, 'Join a server', 'main-menu__title');
        mmEl(head, 'main-menu__header-spacer');
        mmText(head, 'Checked 12 s ago', 'main-menu__fine');
        const refresh = mmEl(head, 'main-menu__icon-btn');
        refresh.title = 'Refresh';
        refresh.dataset.mm = 'toast';
        refresh.dataset.arg = 'In game: every server is asked again';
        refresh.append(h('div', 'main-menu__icon-btn__icon', { icon: 'Heroicons/Solid/Refresh' }));
        mmText(head, 'Esc', 'main-menu__key');
        const close = mmText(head, '×', 'main-menu__icon-btn');
        close.dataset.mm = 'back';
        close.title = 'Back';

        const scroll = mmEl(panel, 'main-menu__server-scroll');
        const content = mmEl(scroll, 'main-menu__server-content');
        const direct = mmEl(content, 'main-menu__section');
        if (mm.first) {
            direct.classList.add('main-menu__first');
            mmText(direct, 'Paste a server address', 'main-menu__first-title');
            mmText(direct, 'Server addresses are shared in the SS3D Discord. Copy one there and paste it here; the menu remembers it after the first join.', 'main-menu__first-copy');
        } else {
            mmText(direct, 'Direct connect', 'main-menu__eyebrow');
        }
        const dr = mmEl(direct, 'main-menu__direct-row');
        mmInput(dr, 'address', mm.address, 'main-menu__address', mm.first ? 'address or address:port' : 'Paste address or address:port');
        const can = hasName && mm.address.trim() !== '';
        mmButton(dr, 'Connect', 'main-menu__button--accent' + (can ? '' : ' main-menu__button--disabled'), 'connect');
        if (mm.first) mmText(direct, 'Open the SS3D Discord', 'main-menu__link').dataset.mm = 'discord';

        const recent = mm.first ? [] : MM_SERVERS;
        const section = (title, list, count, empty) => {
            const sec = mmEl(content, 'main-menu__section');
            const hd = mmEl(sec, 'main-menu__row main-menu__spread main-menu__section-heading');
            mmText(hd, title, 'main-menu__eyebrow');
            mmText(hd, count, 'main-menu__fine');
            if (!list.length) {
                const e = mmEl(sec, 'main-menu__empty');
                mmText(e, empty[0], 'main-menu__empty-title');
                mmText(e, empty[1], 'main-menu__hint');
                return;
            }
            const cols = mmEl(sec, 'main-menu__server-heading');
            [['Server', 'main-menu__server-identity'], ['Status', 'main-menu__status'], ['Players', 'main-menu__players'],
                ['Ping', 'main-menu__ping'], ['', 'main-menu__actions']].forEach(([t, c]) => mmText(cols, t, c));
            let primary = true;
            list.forEach(s => {
                mmServer(sec, s, primary && s.state === 'ok');
                if (s.state === 'ok') primary = false;
            });
        };
        section('Recent', recent, recent.length + ' / 5', ['No recent servers', 'Servers you join show up here, newest first. The last five are kept.']);
        section('Public', [], '0', ['No public servers yet', 'The project lists servers here once they go public. Until then, addresses are shared on Discord.']);
    }

    // PlayerSettingsPanel: the main menu mounts it with its header; the lobby's Settings tab without (showHeader).
    function mmSettings(panel, opts = {}) {
        const sp = mmEl(panel, 'settings-theme settings-panel');
        if (opts.header !== false) {
            const head = mmEl(sp, 'settings-panel__header');
            mmText(head, 'Settings', 'settings-panel__title');
            mmText(head, opts.context || 'Main menu', 'settings-panel__context');
            mmEl(head, 'settings-panel__spacer');
            mmText(head, 'Esc', 'main-menu__key');
            mmButton(head, 'Back', '', 'back');
        }

        const tabs = mmEl(sp, 'settings-panel__tabs');
        Object.keys(MM_SETTINGS).forEach(k => {
            const t = mmText(tabs, k, 'settings-panel__tab' + (mm.tab === k ? ' settings-panel__tab--active' : ''));
            t.dataset.mm = 'tab';
            t.dataset.arg = k;
        });
        const scroll = mmEl(sp, 'settings-panel__scroll');
        const content = mmEl(scroll, 'settings-panel__content');
        const def = MM_SETTINGS[mm.tab];
        const intro = mmEl(content, 'settings-panel__intro-row');
        mmText(intro, def.intro, 'settings-panel__intro');
        mmButton(intro, 'Reset ' + mm.tab, '', 'reset');
        const s = mm.settings;

        if (def.binds) {
            const grid = mmEl(content, 'settings-panel__bind-grid');
            def.binds.forEach(([title, rows]) => {
                const g = mmEl(grid, 'settings-panel__bind-group');
                mmText(g, title, 'settings-panel__group-title');
                rows.forEach(([label, key]) => {
                    const r = mmEl(g, 'settings-panel__row');
                    const copy = mmEl(r, 'settings-panel__row-copy');
                    mmText(copy, label, 'settings-panel__row-label');
                    const listening = mm.listening === label;
                    if (listening) mmText(copy, 'Esc or click again to cancel', 'settings-panel__note-text');
                    const cap = mmText(r, listening ? 'Press a key' : key, 'settings-panel__keycap' + (listening ? ' settings-panel__keycap--listening' : ''));
                    cap.dataset.mm = 'listen';
                    cap.dataset.arg = label;
                });
            });
            return;
        }

        def.groups.forEach(([title, rows]) => {
            const g = mmEl(content, 'settings-panel__group');
            mmText(g, title, 'settings-panel__group-title');
            rows.forEach(d => {
                const r = mmEl(g, 'settings-panel__row');
                const copy = mmEl(r, 'settings-panel__row-copy');
                const line = mmEl(copy, 'settings-panel__row-line');
                mmText(line, d.label, 'settings-panel__row-label');
                if (s[d.key] !== MM_SETTING_DEFAULTS[d.key]) mmEl(line, 'settings-panel__changed').title = 'Changed from default';
                const note = d.note && d.note(s);
                if (note) mmText(copy, note, 'settings-panel__note-text');
                const ctl = mmEl(r, 'settings-panel__control');
                if (d.seg) {
                    const segs = mmEl(ctl, 'settings-panel__segments' + (d.locked && d.locked(s) ? ' settings-panel__segments--locked' : ''));
                    d.seg.forEach((label, i) => {
                        const on = (d.locked && d.locked(s) ? 0 : s[d.key]) === i;
                        const o = mmText(segs, label, 'settings-panel__segment' + (on ? ' settings-panel__segment--on' : ''));
                        if (!(d.locked && d.locked(s))) {
                            o.dataset.mm = 'seg';
                            o.dataset.arg = d.key + ':' + i;
                        }
                    });
                } else if (d.select) {
                    const dd = mmEl(ctl, 'menu-dropdown settings-panel__select');
                    mmText(dd, s[d.key], 'menu-dropdown__value');
                    mmEl(dd, 'menu-dropdown__arrow');
                    const list = mmEl(dd, 'menu-dropdown__list');
                    d.select.forEach(v => mmText(list, v, 'menu-dropdown__item'));
                } else if (d.slider) {
                    const [lo, hi, unit] = d.slider;
                    mmSlider(ctl, (s[d.key] - lo) / (hi - lo), 'settings-panel__slider', d.key);
                    const v = mmText(ctl, s[d.key] + unit, 'settings-panel__slider-value');
                    v.dataset.valueFor = d.key;
                }
            });
        });
    }

    function mmOverlay(root) {
        let kind = mm.overlay === 'connect' ? 'join' : mm.overlay;
        if (mm.lost) kind = 'lost';
        else if (mm.failed) kind = 'failed';
        // In game the dialog stays and its classes change; here it is rebuilt each step, so it eases in only when it
        // first shows (--in a frame later, as MainMenuView does).
        const ov = mmEl(root, 'main-menu__overlay main-menu__overlay--' + kind + (mmOverlayShown ? ' main-menu__overlay--in' : ''));
        ov.id = 'overlay';
        if (!mmOverlayShown) {
            mmOverlayShown = true;
            requestAnimationFrame(() => requestAnimationFrame(() => ov.classList.add('main-menu__overlay--in')));
        }
        const dlg = mmEl(ov, 'main-menu__dialog');
        const head = mmEl(dlg, 'main-menu__header');
        let title = 'Connecting';
        if (mm.overlay === 'update') title = 'Update and restart';
        else if (mm.overlay === 'host') title = 'Starting local session';
        else if (mm.lost) title = 'Connection lost';
        mmGlyph(head, 'main-menu__op-icon', MM_OP_ICONS[kind]);
        mmText(head, title, 'main-menu__title');
        const labels = MM_STEPS[mm.overlay];
        const working = !mm.lost && !mm.failed && mm.step < labels.length;
        const progress = mmEl(dlg, 'main-menu__progress' + (mm.lost ? ' main-menu__progress--hidden' : '')
            + (working ? ' main-menu__progress--working' : ''));
        const fill = mmEl(progress, 'main-menu__progress-fill');
        mmEl(fill, 'main-menu__progress-glint');
        // Done steps and half the working one; a failure fills up to the step that failed. From the last share, eased.
        const share = Math.min(1, (mm.step + (mm.failed ? 1 : 0.5)) / labels.length) * 100;
        fill.style.width = mmLastShare + '%';
        requestAnimationFrame(() => requestAnimationFrame(() => { fill.style.width = share + '%'; }));
        mmLastShare = share;
        const body = mmEl(dlg, 'main-menu__dialog-body');
        const local = mm.overlay === 'host';
        mmText(body, local ? 'Your session' : mm.overlay === 'update' ? 'SS3D v0.5.0' : MM_SERVERS[0].name, 'main-menu__overlay-name');
        mmText(body, local ? `127.0.0.1:${mm.port}` : mm.overlay === 'update' ? 'github.com/RE-SS3D/SS3D/releases' : MM_SERVERS[0].address, 'main-menu__overlay-address');
        const steps = mmEl(body, 'main-menu__steps');
        steps.id = 'progress-steps';
        if (mm.lost) {
            mmText(steps, 'The server stopped responding. It may have restarted or gone offline.', 'main-menu__step-reason');
        } else {
            MM_STEPS[mm.overlay].forEach((label, i) => {
                const failed = mm.failed && i === mm.step;
                const done = i < mm.step;
                const active = !mm.failed && i === mm.step;
                const state = failed ? 'failed' : done ? 'done' : active ? 'active' : 'pending';
                const row = mmEl(steps, 'main-menu__step' + (i > mm.step ? ' main-menu__step--pending' : '')
                    + (active ? ' main-menu__step--active' : '') + (failed ? ' main-menu__step--failed' : ''));
                const icon = MM_STEP_ICONS[state]
                    ? mmGlyph(row, 'main-menu__step-icon main-menu__step-icon--' + state, MM_STEP_ICONS[state])
                    : mmEl(row, 'main-menu__step-icon main-menu__step-icon--pending');
                // The check that just appeared pops (in game: a scale transition from the spinner's size).
                if (done && i === mm.step - 1) icon.classList.add('is-new');
                mmText(row, label, 'main-menu__step-label');
                let mark = '';
                let cls = '';
                if (done) { mark = 'OK'; cls = ' main-menu__step-mark--ok'; }
                else if (failed) { mark = 'Failed'; cls = ' main-menu__step-mark--failed'; }
                mmText(row, mark, 'main-menu__step-mark' + cls);
                if (failed) mmText(steps, 'The server runs v0.5.0; this is v0.4.2. Update to join it.', 'main-menu__step-reason');
            });
        }
        const actions = mmEl(body, 'main-menu__row main-menu__end');
        const done = mm.failed || mm.lost;
        if (!done && mm.overlay !== 'update') mmButton(actions, 'Cancel', '', 'cancel');
        if (done) {
            mmButton(actions, 'Back to servers', '', 'back-to-servers');
            mmButton(actions, 'Retry', 'main-menu__button--accent main-menu__dialog-action', 'retry');
        }
    }

    function mmRender() {
        const cv = $('#mm-canvas');
        if (!cv) return;
        const focus = document.activeElement && cv.contains(document.activeElement) ? document.activeElement : null;
        const field = focus && focus.dataset.field;
        const caret = focus && focus.selectionStart;
        clearInterval(mmTimer);

        if (!mm.overlay) {
            mmOverlayShown = false;
            mmLastShare = 0;
        }
        let root = $('.main-menu', cv);
        if (!root) {
            root = h('div', 'main-menu');
            cv.append(root);
        }
        // The wallpaper stays across renders, so it doesn't reload; everything else is rebuilt from the state.
        let wall = $('.main-menu__wallpaper', root);
        if (!wall) {
            wall = h('div', 'main-menu__wallpaper');
            root.append(wall);
        }
        wall.style.backgroundImage = cssUrl('Wallpapers/' + MM_WALLPAPER);
        wall.classList.toggle('main-menu__wallpaper--blurred', !!mm.overlay);
        $$(':scope > :not(.main-menu__wallpaper)', root).forEach(e => e.remove());

        if (mm.overlay) {
            mmOverlay(root);
        } else {
            mmSidebar(root);
            if (mm.screen !== 'home') {
                const panel = mmEl(root, 'main-menu__panel');
                panel.id = mm.screen === 'settings' ? 'settings-host' : 'side-panel';
                if (mm.screen === 'servers') mmServers(panel);
                else mmSettings(panel);
            }
        }
        $$('[data-mm-chip]').forEach(c => c.classList.toggle('is-on', c.dataset.mmChip === mm.chip));

        if (field) {
            const again = $(`[data-field="${field}"]`, cv);
            if (again) {
                again.focus();
                if (caret != null) again.setSelectionRange(caret, caret);
            }
        }
        // A running operation steps on by itself; at the end the lobby would open.
        if (mm.overlay && !mm.failed && !mm.lost && mm.chip === null) {
            mmTimer = setInterval(() => {
                mm.step++;
                if (mm.step >= MM_STEPS[mm.overlay].length) {
                    clearInterval(mmTimer);
                    toast(mm.overlay === 'update' ? 'In game: the new version starts, on the menu' : 'In game: the lobby opens');
                    Object.assign(mm, { overlay: null, step: 0, screen: 'home' });
                }
                mmRender();
            }, 650);
        }
    }

    function mmState(name) {
        Object.assign(mm, MM_BASE, MM_STATES[name], { chip: name });
        mmRender();
        syncOld('#mm-stage', name === 'settings' ? 'lobby-settings' : 'main-menu-' + name.replace(/ /g, '-'));
    }

    // While comparing, a state chip shows the old design's capture of the same state, or its nearest.
    function syncOld(stageSel, img) {
        const host = $(stageSel);
        const st = host && host._cmp;
        if (!st) return;
        const i = st.refs.findIndex(r => r.img === 'old/' + img);
        if (i >= 0 && i !== st.i) setRef(host, i);
    }

    // The settings panel's own actions (tabs, options, reset, key capture), shared by the main menu and the lobby.
    function settingsAct(action, arg) {
        switch (action) {
            case 'tab':
                mm.tab = arg;
                mm.listening = null;
                return true;
            case 'seg': {
                const [k, i] = arg.split(':');
                mm.settings[k] = Number(i);
                return true;
            }
            case 'reset':
                Object.keys(mm.settings).forEach(k => {
                    const inTab = MM_SETTINGS[mm.tab].groups && MM_SETTINGS[mm.tab].groups.some(([, rows]) => rows.some(r => r.key === k));
                    if (inTab) mm.settings[k] = MM_SETTING_DEFAULTS[k];
                });
                if (mm.tab === 'controls') toast('In game: every binding goes back to its default');
                return true;
            case 'listen':
                mm.listening = mm.listening === arg ? null : arg;
                return true;
            default:
                return false;
        }
    }

    // A menu slider under the pointer. Sliders are rebuilt on each render, so their canvas drives them.
    function dragMenuSlider(e, apply, done) {
        const sl = e.target.closest('[data-mm-slider]');
        if (!sl) return;
        const move = ev => {
            const r = sl.getBoundingClientRect();
            const v = Math.max(0, Math.min(1, (ev.clientX - r.left) / r.width));
            setSlider(sl, v);
            apply(sl.dataset.mmSlider, v);
        };
        sl.setPointerCapture(e.pointerId);
        sl.classList.add('menu-slider--dragging');
        move(e);
        const up = () => {
            sl.classList.remove('menu-slider--dragging');
            sl.removeEventListener('pointermove', move);
            sl.removeEventListener('pointerup', up);
            if (done) done(sl.dataset.mmSlider);
        };
        sl.addEventListener('pointermove', move);
        sl.addEventListener('pointerup', up);
    }

    // A settings slider sets its value in steps of 5 and updates its label in place.
    function settingFromSlider(cv, key, v) {
        const d = Object.values(MM_SETTINGS).flatMap(t => (t.groups || []).flatMap(([, rows]) => rows)).find(x => x.key === key);
        if (!d) return;
        const [lo, hi, unit] = d.slider;
        mm.settings[key] = Math.round((lo + v * (hi - lo)) / 5) * 5;
        const label = $(`[data-value-for="${key}"]`, cv);
        if (label) label.textContent = mm.settings[key] + unit;
    }

    function mmAct(action, arg) {
        mm.chip = null;
        if (settingsAct(action, arg)) {
            mmRender();
            return;
        }
        const hasName = mm.name.trim() !== '';
        switch (action) {
            case 'join':
                if (!hasName) return;
                mm.screen = mm.screen === 'servers' ? 'home' : 'servers';
                break;
            case 'settings':
                mm.screen = mm.screen === 'settings' ? 'home' : 'settings';
                break;
            case 'back':
                mm.screen = 'home';
                break;
            case 'local':
                if (!hasName) return;
                Object.assign(mm, { overlay: 'host', step: 0, failed: false, lost: false });
                break;
            case 'quit':
                toast('In game: the game closes at once (no confirm)');
                return;
            case 'advanced':
                mm.advanced = !mm.advanced;
                break;
            case 'music':
                mm.music = !mm.music;
                break;
            case 'next':
                mm.track = (mm.track + 1) % MM_TRACKS.length;
                mm.music = true;
                break;
            case 'discord':
            case 'credits':
                toast(action === 'discord' ? 'In game: opens the SS3D Discord' : 'In game: opens the SS3D-Art repository');
                return;
            case 'details':
                mm.expanded = mm.expanded === arg ? null : arg;
                break;
            case 'show-token': {
                const t = $('#mm-canvas [data-field="token"]');
                if (t) t.type = t.type === 'password' ? 'text' : 'password';
                return;
            }
            case 'join-server':
            case 'connect':
            case 'retry':
                Object.assign(mm, { overlay: 'connect', step: 0, failed: false, lost: false });
                break;
            case 'overlay':
                Object.assign(mm, { overlay: arg, step: 0, failed: false, lost: false });
                break;
            case 'cancel':
            case 'back-to-servers':
                Object.assign(mm, { overlay: null, step: 0, failed: false, lost: false, screen: action === 'cancel' && mm.overlay === 'host' ? 'home' : 'servers' });
                break;
            case 'toast':
                toast(arg);
                return;
            default:
                return;
        }
        mmRender();
    }

    function buildMainMenu() {
        const cv = $('#mm-canvas');
        if (!cv) return;
        const chips = $('[data-mm-chips]');
        Object.keys(MM_STATES).forEach(name => {
            const b = h('button', 'chip-btn', { text: name });
            b.dataset.mmChip = name;
            b.addEventListener('click', () => mmState(name));
            chips.append(b);
        });

        cv.addEventListener('click', e => {
            const t = e.target.closest('[data-mm]');
            if (!t || t.classList.contains('main-menu__button--disabled')) return;
            mmAct(t.dataset.mm, t.dataset.arg);
        });
        cv.addEventListener('input', e => {
            const f = e.target.dataset.field;
            if (!f) return;
            if (f === 'name' || f === 'port' || f === 'address') {
                mm[f] = e.target.value;
                mm.chip = null;
                mmRender();
            }
        });
        cv.addEventListener('keydown', e => {
            if (e.key === 'Enter' && e.target.dataset.field === 'address' && mm.address.trim() && mm.name.trim()) mmAct('connect');
        });
        cv.addEventListener('pointerdown', e => dragMenuSlider(e, (key, v) => {
            if (key === 'volume') mm.volume = v;
            else settingFromSlider(cv, key, v);
        }, key => {
            if (key !== 'volume') mmRender(); // the changed-from-default dot
        }));
        // Resolution: the dropdown is wired page-wide (wireMenus); keep its pick in the state.
        cv.addEventListener('click', e => {
            const item = e.target.closest('.settings-panel__select .menu-dropdown__item');
            if (item) mm.settings.resolution = item.textContent;
        });

        // Esc is Back while the pointer is over the screen, as in game.
        let over = false;
        cv.addEventListener('pointerenter', () => { over = true; });
        cv.addEventListener('pointerleave', () => { over = false; });
        document.addEventListener('keydown', e => {
            if (!over || e.key !== 'Escape') return;
            if (mm.listening) mmAct('listen', mm.listening);
            else if (mm.overlay && !mm.failed && !mm.lost && mm.overlay !== 'update') mmAct('cancel');
            else if (mm.screen !== 'home') mmAct('back');
        });
        mmState('home');
    }

    // ══════════════════════════════════════════════════════════════════════
    // Lobby: LobbyView's tree (with the tab views, the Join picker and the switcher) in the main menu's look
    // ══════════════════════════════════════════════════════════════════════
    const LB_ICON = {
        lock: 'Heroicons/Outline/LockClosed', info: 'Heroicons/Outline/InformationCircle', warning: 'Heroicons/Outline/Exclamation',
        chevron: 'Heroicons/Outline/ChevronRight', down: 'Heroicons/Outline/ChevronDown', up: 'Heroicons/Outline/ChevronUp', users: 'Heroicons/Outline/Users',
        check: 'Heroicons/Outline/Check', tick: 'Heroicons/Solid/Check', user: 'Heroicons/Solid/User',
        ghost: 'External/lorc/ghost.svg', bug: 'External/lorc/long-antennae-bug.svg', logout: 'Heroicons/Outline/Logout',
        music: 'Heroicons/Solid/MusicNote', next: 'Heroicons/Solid/FastForward', external: 'Heroicons/Outline/ExternalLink',
        send: 'Heroicons/Outline/PaperAirplane', duplicate: 'Heroicons/Outline/Duplicate', trash: 'Heroicons/Outline/Trash',
        plus: 'Heroicons/Outline/Plus', x: 'Heroicons/Outline/X',
    };
    // Department symbols (game-icons.net, CC BY 3.0), as Lobby.uss's lobby__icon--dept-*.
    const LB_DEPT_ICON = {
        command: 'External/delapouite/captain-hat-profile.svg', security: 'External/delapouite/police-target.svg',
        engineering: 'External/lorc/cog.svg', medical: 'External/lorc/hospital-cross.svg', science: 'External/skoll/atom.svg',
        cargo: 'External/delapouite/cargo-crate.svg', service: 'External/lorc/beer-stein.svg', civilian: 'External/delapouite/bed.svg',
    };
    const LB_DEPTS = Object.keys(LB_DEPT_ICON);
    const LB_JOBS = {
        Captain: 'command', 'Head of Personnel': 'command', 'Head of Security': 'security', 'Security Officer': 'security',
        Detective: 'security', 'Chief Engineer': 'engineering', Engineer: 'engineering', 'Atmospheric Technician': 'engineering',
        Doctor: 'medical', Chemist: 'medical', Scientist: 'science', Quartermaster: 'cargo', 'Cargo Technician': 'cargo',
        Bartender: 'service', Chef: 'service', Janitor: 'service', Assistant: 'civilian',
    };
    const LB_CHARS = [
        { name: 'Rory Lindqvist', high: 'Security Officer' },
        { name: 'Ada Brandt', high: 'Doctor' },
        { name: 'Mags Okafor', high: null },
    ];
    const LB_NAMES = ['Tamsin Reyes', 'Ezra Holm', 'Noor Vance', 'Felix Adeyemi', 'Wren Castillo', 'Idris Kowal', 'Petra Lund'];
    // Open jobs during a round: open / total slots ('' is uncapped), and the description the picked one shows.
    const LB_OPEN = [
        ['Doctor', '2/2', 'Treats the injured and the sick in medbay: bandages, pills, surgery. Brings the dead back while it still can.'],
        ['Engineer', '1/4', 'Keeps the station powered and airtight: the engine, the wiring, the breaches. Has access to most maintenance.'],
        ['Cargo Technician', '1/2', 'Orders, moves and delivers the station\'s supplies from the cargo bay.'],
        ['Assistant', '', 'No department and no special access. Helps out wherever someone will have them.'],
    ];
    const LB_CREW = [
        ['Hal Mercer', 'Captain'], ['Juno Park', 'Head of Personnel'], ['Ines Duarte', 'Head of Security'],
        ['Theo Brandt', 'Security Officer'], ['Sam Achebe', 'Detective'], ['Oskar Vale', 'Chief Engineer'], ['Lia Moreau', 'Engineer'],
        ['Pip Hollis', 'Atmospheric Technician'], ['Ana Sousa', 'Chemist'], ['Remy Laine', 'Scientist'], ['Bo Kessler', 'Quartermaster'],
        ['Nell Ortiz', 'Bartender'], ['Ike Brandvold', 'Chef'], ['Dot Pruitt', 'Assistant'],
    ];
    const LB_CHAT = [
        { t: '20:58', ch: 'ooc', sys: 'Theo Brandt connected.' },
        { t: '20:59', ch: 'ooc', who: 'Juno', text: 'anyone up for engineering this round?' },
        { t: '21:00', ch: 'ooc', who: 'Oskar', text: "I'll take Chief Engineer if two of you come along" },
        { t: '21:01', ch: 'looc', who: 'Pip', text: 'spare toolbox is by the lockers, grab it on your way' },
        { t: '21:02', ch: 'ooc', who: 'Hal', admin: true, text: 'Secret tonight. We start when the timer runs out, play nice.' },
        { t: '21:02', ch: 'admin', who: 'Hal', admin: true, text: 'holding a minute if Lia is still loading' },
        { t: '21:03', ch: 'ooc', who: 'Ada', text: 'o7' },
    ];
    const LB_MOTD = 'Welcome to the RE:SS3D sandbox.\n\nRounds start on their own once four players are connected. Secret is on: anyone could be an operative, so lock your doors.\n\nFound a bug? F8 files it with your log attached. Be kind in OOC; the admins are on the Discord.';
    // Sample entries: the change log is the client build's merged PR titles under the release that shipped them.
    const LB_CHANGES = [
        ['v0.4.2', '12 Nov 2026', [['Station lighting is flat and even, no dark corners', '#1551'], ['Department colours: darker Command, Security and Science', '#1550'], ['Map editor: Map Check opens from the warning cell', '#1547']]],
        ['v0.4.1', '30 Oct 2026', [['Late joiners pick a job beside the crew manifest', '#1532'], ['Observe goes in as a ghost at once', '#1524'], ['Lobby chat keeps the last 200 lines', '#1519']]],
    ];
    const LB_LAST_CREW = [
        { name: 'Hal Mercer', job: 'Captain', alive: false }, { name: 'Rory Lindqvist', job: 'Security Officer', alive: false, you: true },
        { name: 'Ines Duarte', job: 'Head of Security', alive: true }, { name: 'Oskar Vale', job: 'Chief Engineer', alive: true },
        { name: 'Lia Moreau', job: 'Engineer', alive: false }, { name: 'Ana Sousa', job: 'Chemist', alive: true },
        { name: 'Remy Laine', job: 'Scientist', alive: true }, { name: 'Bo Kessler', job: 'Quartermaster', alive: false },
        { name: 'Nell Ortiz', job: 'Bartender', alive: true }, { name: 'Ike Brandvold', job: 'Chef', alive: true },
        { name: 'Dot Pruitt', job: 'Assistant', alive: true },
    ];
    // The clock each round state starts from, in seconds.
    const LB_SECS = { countdown: 161, held: 72, starting: 45, ongoing: 2832, ending: 24 };
    const LB_BASE = {
        round: 'countdown', ready: false, spawned: false, lateJoin: true, autoStart: true, endHeld: false, tab: 'info',
        adminOn: false, join: false, picked: null, onlyFallback: false, filled: null, switcher: false, confirmDelete: null,
        prompt: false, quitArmed: false, channel: 'ooc', noLast: false, mapName: 'Box Station (edited)',
    };
    // One chip per state LobbyView shows (the lobby canvas's sidebar states and boards, roughly).
    const LB_STATES = {
        'pre-round': {},
        'ready': { ready: true },
        'waiting for players': { round: 'waiting' },
        'countdown held': { round: 'held' },
        'loading': { round: 'loading' },
        'starting': { round: 'starting', ready: true },
        'in progress': { round: 'ongoing' },
        'late join off': { round: 'ongoing', lateJoin: false },
        'escape menu': { round: 'ongoing', spawned: true },
        'round over': { round: 'ending', tab: 'last' },
        'join': { round: 'ongoing', join: true, picked: 'Doctor' },
        'join: job filled': { round: 'ongoing', join: true, filled: 'Doctor' },
        'join: only assistant': { round: 'ongoing', join: true, onlyFallback: true },
        'switch character': { switcher: true },
        'delete character': { switcher: true, confirmDelete: 2 },
        'chat': { tab: 'chat' },
        'settings': { tab: 'settings' },
        'last round': { tab: 'last' },
        'server settings': { tab: 'admin', adminOn: true },
        'unsaved map': { round: 'ongoing', spawned: true, tab: 'admin', adminOn: true, prompt: true },
    };
    const lb = { ...LB_BASE, chip: 'pre-round', secs: LB_SECS.countdown, chars: [], selected: 0, chat: [], draft: '',
        music: true, track: 0, volume: 0.5, fields: {}, layout: 'b' };
    // Layout B (the character stage): these tabs need the room, so the panel widens over the stage while they're open.
    const LB_WIDE_TABS = ['settings', 'last', 'admin'];
    const lbStageLayout = () => lb.layout === 'b';
    let lbQuitTimer = 0;

    const lbClock = s => `${String(Math.floor(Math.max(0, s) / 60)).padStart(2, '0')}:${String(Math.max(0, s) % 60).padStart(2, '0')}`;
    const lbLong = s => `${String(Math.floor(s / 3600)).padStart(2, '0')}:${lbClock(s % 3600)}`;
    const lbCap = s => s[0].toUpperCase() + s.slice(1);
    const lbIdle = () => ['countdown', 'held', 'waiting', 'manual'].includes(lb.round);

    function lbText(parent, text, cls) {
        const e = h('div', 'lobby__text' + (cls ? ' ' + cls : ''), { text });
        parent.append(e);
        return e;
    }

    function lbEl(parent, cls) {
        const e = h('div', cls);
        parent.append(e);
        return e;
    }

    // A white glyph, tinted by its rule's background colour (web-only mask; USS: background-image and tint).
    function lbIcon(parent, key, cls) {
        const e = h('div', 'lobby__icon' + (cls ? ' ' + cls : ''));
        const u = cssUrl(LB_ICON[key] || key);
        e.style.webkitMaskImage = u;
        e.style.maskImage = u;
        parent.append(e);
        return e;
    }

    function lbOn(el, action, arg) {
        el.dataset.lb = action;
        if (arg != null) el.dataset.arg = arg;
        return el;
    }

    // LobbySidebarModel.Round: the round section's words, readout and note.
    function lbRound() {
        const s = lb.secs;
        const base = { status: 'Pre-round', tone: 'info', caption: 'Round starts in', timer: lbClock(s), held: false, note: '', warn: false };
        switch (lb.round) {
            case 'loading': return { ...base, status: 'Loading', tone: 'warning', caption: 'Loading the station', timer: '' };
            case 'starting': return { ...base, status: 'Starting', tone: 'warning' };
            case 'ongoing': return { ...base, status: 'In progress', tone: 'live', caption: 'Round time', timer: lbLong(s) };
            case 'ending': return { ...base, status: 'Round over', tone: 'warning', caption: 'Back to the lobby in',
                timer: lb.endHeld ? '' : lbClock(s), note: lb.endHeld ? 'The summary is held by an admin' : '', warn: true };
            case 'held': return { ...base, tone: 'warning', held: true, note: 'Countdown held by an admin', warn: true };
            case 'waiting': return { ...base, tone: '', caption: 'Waiting for players', timer: '', note: '3/4 connected. The countdown starts at 4.' };
            case 'manual': return { ...base, tone: '', caption: 'Waiting for an admin', timer: '', note: 'An admin starts the round on this server.' };
            default: return base;
        }
    }

    // LobbySidebarModel.Entry and .Observe.
    function lbEntry() {
        const first = lb.chars[lb.selected].name.trim().split(' ')[0] || 'your character';
        const off = (title, subtitle, note) => ({ title, subtitle, look: 'disabled', note });
        if (lb.round === 'ongoing') {
            if (lb.spawned) return off('In round', 'You already have a body this round');
            if (!lb.lateJoin) return off('Join', 'Late joining is off', 'An admin has turned off late joining for this round. You can still observe.');
            return { title: 'Join', subtitle: 'Pick an open job and spawn now', look: 'accent', action: 'join' };
        }
        if (lb.round === 'ending') return off('Ready up', 'The round is ending');
        if (lb.spawned) return off('In round', 'You already have a body this round');
        if (lb.ready) return { title: 'Ready', subtitle: "You'll spawn at round start. Click to cancel", look: 'confirmed', action: 'ready' };
        return { title: 'Ready up', subtitle: `Spawn as ${first} when the round starts`, look: 'accent', action: 'ready' };
    }

    function lbObserve() {
        if (lb.spawned) return { subtitle: "You're already in the round" };
        if (lb.round === 'starting' || lb.round === 'ongoing') return { subtitle: 'Go in now as a ghost, no body', enabled: true };
        return { subtitle: 'Opens once the station has loaded' };
    }

    function lbNote(parent, text, warn, cls) {
        const n = lbEl(parent, 'lobby__row lobby__note' + (warn ? ' lobby__note--warning' : '') + (cls ? ' ' + cls : ''));
        lbIcon(n, warn ? 'lock' : 'info', 'lobby__note-icon');
        lbText(n, text, 'lobby__note-text');
    }

    function lbSidebar(root) {
        const side = lbEl(root, 'lobby__sidebar');
        side.id = 'lobby-sidebar';
        const mark = lbEl(lbEl(side, 'lobby__wordmark-block'), 'lobby__wordmark');
        mark.style.backgroundImage = cssUrl('Logos/SS3DLogo');
        mark.title = 'Space Station 3D';

        const r = lbRound();
        const tone = base => (r.tone ? ` ${base}--${r.tone}` : '');
        const round = lbEl(side, 'lobby__section lobby__round lb-tone--' + (r.tone || 'neutral') + (r.timer ? '' : ' lobby__round--waiting'));
        round.id = 'lobby-round';
        const head = lbEl(round, 'lobby__row');
        lbText(head, 'Round', 'lobby__eyebrow');
        lbEl(head, 'lobby__grow');
        lbEl(head, 'lobby__dot' + tone('lobby__dot'));
        lbText(head, r.status, 'lobby__status' + tone('lobby__status'));
        lbText(round, r.caption, 'lobby__round-caption');
        if (r.timer) {
            const wrap = lbEl(round, 'lobby__timer-wrap' + (r.held ? ' lobby__timer-wrap--held' : ''));
            const t = lbText(wrap, r.timer, 'lobby__timer');
            if (!r.held) t.dataset.lbTimer = '';
            lbEl(wrap, 'lobby__timer-strike');
        }
        if (r.note) lbNote(round, r.note, r.warn);
        if (lb.round === 'loading') lbText(round, 'Loading map: Box Station…', 'lobby__map-load lobby__map-load--loading');

        if (!lbStageLayout()) lbCharacterCard(side);

        const e = lbEntry();
        const entry = lbEl(side, 'lobby__section lobby__entry');
        const ready = lbEl(entry, 'lobby__ready' + (e.look === 'accent' ? '' : ' lobby__ready--' + e.look));
        ready.id = 'lobby-ready';
        if (e.action) lbOn(ready, e.action);
        lbIcon(ready, 'check', 'lobby__ready-check');
        const rt = lbEl(ready, 'lobby__ready-text');
        lbText(rt, e.title, 'lobby__ready-title');
        lbText(rt, e.subtitle, 'lobby__ready-subtitle');
        if (e.note) lbNote(entry, e.note, false, 'lobby__entry-note lobby__entry-note--shown');
        const o = lbObserve();
        const obs = lbEl(entry, 'lobby__observe' + (o.enabled ? '' : ' lobby__observe--disabled'));
        if (o.enabled) lbOn(obs, 'observe');
        lbIcon(obs, 'ghost', 'lobby__observe-icon');
        const ot = lbEl(obs, 'lobby__observe-text');
        lbText(ot, 'Observe', 'lobby__observe-title');
        lbText(ot, o.subtitle, 'lobby__observe-subtitle');

        lbEl(side, 'lobby__spacer');
        lbMusic(side);
        lbFooter(side);
    }

    // Layout A: the character card under the round, with Switch character under it.
    function lbCharacterCard(side) {
        const c = lb.chars[lb.selected];
        const who = lbEl(side, 'lobby__section lobby__character');
        const card = lbOn(lbEl(who, 'lobby__card' + (c.high ? ' lobby__card--dept lb-dept--' + LB_JOBS[c.high] : '')), 'card');
        card.title = 'Open the character window';
        lbIcon(lbEl(card, 'lobby__preview-frame'), 'user', 'lobby__preview-image');
        const text = lbEl(card, 'lobby__card-text');
        lbText(text, 'Playing as', 'lobby__eyebrow');
        lbText(text, c.name, 'lobby__character-name');
        const high = lbEl(text, 'lobby__row lobby__high');
        if (c.high) {
            lbEl(high, 'lb-swatch lb-dept--' + LB_JOBS[c.high]);
            lbText(high, 'High:', 'lobby__high-prefix');
            lbText(high, c.high, 'lobby__high-job lb-dept-text');
        } else {
            lbText(high, 'No job preferences', 'lobby__high-prefix');
        }
        lbEl(text, 'lobby__grow');
        const edit = lbEl(text, 'lobby__row lobby__card-edit');
        lbText(edit, 'Edit character', 'lobby__card-edit-label');
        lbIcon(edit, 'chevron');
        lbSwitchRow(who);
    }

    function lbSwitchRow(parent) {
        const open = lb.switcher && !lb.join;
        const sw = lbOn(lbEl(parent, 'lobby__switch' + (open ? ' lobby__switch--open' : '')), 'switcher');
        sw.id = 'lobby-switch-character';
        sw.title = 'Switch to another of your characters';
        lbIcon(sw, 'users');
        lbText(sw, 'Switch character', 'lobby__switch-label');
        lbEl(sw, 'lobby__grow');
        lbText(sw, `${lb.chars.length}/10`, 'lobby__switch-count');
        lbIcon(sw, open && lbStageLayout() ? 'up' : 'down', 'lobby__icon--chevron');
        lbEl(sw, 'lobby__switch-tip');
        return sw;
    }

    function lbMusic(side) {
        const music = lbEl(side, 'lobby__music' + (lb.music ? '' : ' lobby__music--off'));
        const toggle = lbOn(lbEl(music, 'lobby__music-toggle' + (lb.music ? ' lobby__music-toggle--on' : '')), 'music');
        toggle.title = lb.music ? 'Turn music off' : 'Turn music on';
        lbIcon(toggle, 'music');
        const mt = lbEl(music, 'lobby__music-text');
        lbText(mt, 'Music', 'lobby__eyebrow');
        lbText(mt, lb.music ? MM_TRACKS[lb.track] : 'Off', 'lobby__music-title');
        const next = lbOn(lbEl(music, 'lobby__music-next'), 'next');
        next.title = 'Next track';
        lbIcon(next, 'next');
        mmSlider(music, lb.volume, 'lobby__music-volume', 'volume');
    }

    function lbFooter(side) {
        const foot = lbEl(side, 'lobby__footer');
        const bug = lbOn(lbEl(foot, 'lobby__quiet-button lobby__footer-button'), 'bug');
        bug.title = 'Report a bug (F8)';
        lbIcon(bug, 'bug');
        lbText(bug, 'Report a bug', 'lobby__footer-label');
        lbText(bug, 'F8', 'lobby__keycap');
        const quit = lbOn(lbEl(foot, 'lobby__quiet-button lobby__footer-button lobby__quit' + (lb.quitArmed ? ' lobby__quit--armed' : '')), 'quit');
        lbIcon(quit, 'logout');
        lbText(quit, lb.quitArmed ? 'Click again to quit' : 'Quit to menu', 'lobby__footer-label');
    }

    // Layout B: the character you play, large, between the narrow sidebar and the narrow panel. In game the booth's
    // live render texture (CharacterPreviewBooth); here a still of it, taken in the Editor, for every character.
    function lbStage(root) {
        const c = lb.chars[lb.selected];
        const dept = c.high ? LB_JOBS[c.high] : null;
        const covered = LB_WIDE_TABS.includes(lb.tab) ? ' lobby__stage--covered' : '';
        const st = lbEl(root, 'lobby__stage' + (dept ? ' lb-dept--' + dept : ' lobby__stage--neutral') + covered);
        st.id = 'lobby-character-stage';

        const plate = lbEl(st, 'lobby__stage-plate');
        lbText(plate, 'Playing as', 'lobby__eyebrow');
        lbText(plate, c.name, 'lobby__stage-name');
        const high = lbEl(plate, 'lobby__row lobby__stage-high');
        if (c.high) {
            lbIcon(high, LB_DEPT_ICON[dept], 'lobby__stage-dept-icon lb-tint');
            lbText(high, 'High:', 'lobby__high-prefix');
            lbText(high, c.high, 'lobby__high-job lb-dept-text');
        } else {
            lbText(high, 'No job preferences', 'lobby__high-prefix');
        }
        lbEl(plate, 'lobby__stage-accent');

        const fig = lbOn(lbEl(st, 'lobby__stage-figure'), 'card');
        fig.id = 'lobby-preview-image';
        fig.title = 'Open the character window';
        lbEl(fig, 'lobby__stage-floor');
        lbEl(fig, 'lobby__stage-shadow');
        lbEl(fig, 'lobby__stage-model').style.backgroundImage = 'url(renders/lobby-character.png)'; /* web-only stand-in */

        const bar = lbEl(st, 'lobby__stage-bar');
        lbSwitchRow(bar);
        const edit = lbOn(lbEl(bar, 'lobby__stage-edit'), 'card');
        edit.title = 'Open the character window';
        lbText(edit, 'Edit character', 'lobby__card-edit-label');
        lbIcon(edit, 'chevron');
    }

    function lbMain(root) {
        const stage = lbStageLayout();
        const main = lbEl(root, 'lobby__main' + (stage && LB_WIDE_TABS.includes(lb.tab) ? ' lobby__main--wide' : ''));
        main.id = 'lobby-shell-window';
        if (stage) lbStackedHeader(main);
        else lbHeader(main);

        const tabs = lbEl(main, 'lobby__tabs');
        tabs.id = 'lobby-tabs';
        const names = stage
            ? [['info', 'Info'], ['chat', 'Chat'], ['settings', 'Settings'], ['last', 'Last round'], ['admin', 'Server']]
            : [['info', 'Server info'], ['chat', 'Chat'], ['settings', 'Settings'], ['last', 'Last round'], ['admin', 'Server settings']];
        names.forEach(([id, label]) => {
            let cls = 'lobby__tab' + (lb.tab === id ? ' lobby__tab--active' : '');
            if (id === 'admin' && !lb.adminOn) cls += ' lobby__tab--hidden';
            const t = lbOn(lbText(tabs, id === 'admin' ? '' : label, cls), 'tab', id);
            if (id === 'admin') {
                lbText(t, label, 'lobby__tab-label');
                lbText(t, 'Admin', 'lobby__tab-badge');
            }
        });
        if (!stage) {
            lbEl(tabs, 'lobby__grow');
            lbAdminSwitch(tabs);
        }

        const content = lbEl(main, 'lobby__tab-content');
        content.id = 'lobby-tab-content';
        if (lb.tab === 'info') lbInfo(content);
        else if (lb.tab === 'chat') lbChat(content);
        else if (lb.tab === 'settings') mmSettings(lbEl(content, 'lobby__settings-host'), { header: false });
        else if (lb.tab === 'last') lbLast(content);
        else lbAdmin(content);
    }

    function lbAdminSwitch(parent) {
        const admin = lbEl(parent, 'lobby__admin');
        lbText(admin, 'Admin', 'lobby__admin-label');
        const sw = lbOn(lbEl(admin, 'lobby__switch-toggle' + (lb.adminOn ? ' lobby__switch-toggle--on' : '')), 'admin');
        sw.title = lb.adminOn ? 'Hide Server Settings' : 'Show Server Settings';
        lbEl(sw, 'lobby__switch-knob');
    }

    function lbBack(parent) {
        const back = lbOn(lbEl(parent, 'lobby__quiet-button lobby__back' + (lb.spawned ? ' lobby__back--shown' : '')), 'back');
        back.title = 'Close this menu (Esc)';
        lbText(back, 'Back to round', 'lobby__back-label');
        lbText(back, 'Esc', 'lobby__keycap');
    }

    function lbRoundMeta(meta) {
        const r = lbRound();
        lbEl(meta, 'lobby__dot' + (r.tone ? ' lobby__dot--' + r.tone : ''));
        lbText(meta, r.status, 'lobby__meta-value' + (r.tone ? ' lobby__status--' + r.tone : ''));
    }

    // Layout B: map and mode live in Info's server table; the header keeps players and the round.
    function lbStackedHeader(main) {
        const head = lbEl(main, 'lobby__header lobby__header--stacked');
        const top = lbEl(head, 'lobby__header-row');
        lbText(top, 'RE:SS3D Sandbox', 'lobby__server-name');
        lbEl(top, 'lobby__grow');
        lbBack(top);
        const bottom = lbEl(head, 'lobby__header-row');
        const meta = lbEl(bottom, 'lobby__header-meta');
        lbText(meta, 'Players', 'lobby__meta-key');
        lbText(meta, lb.round === 'waiting' ? '3/40' : '14/40', 'lobby__meta-value');
        lbEl(meta, 'lobby__meta-sep');
        lbText(meta, 'Round', 'lobby__meta-key');
        lbRoundMeta(meta);
        lbEl(bottom, 'lobby__grow');
        lbAdminSwitch(bottom);
    }

    function lbHeader(main) {
        const head = lbEl(main, 'lobby__header');
        lbText(head, 'RE:SS3D Sandbox', 'lobby__server-name');
        const meta = lbEl(head, 'lobby__header-meta');
        const pair = (k, v, sep = true) => {
            if (sep) lbEl(meta, 'lobby__meta-sep');
            lbText(meta, k, 'lobby__meta-key');
            if (v != null) lbText(meta, v, 'lobby__meta-value');
        };
        pair('Map', 'Box Station', false);
        pair('Mode', 'Secret');
        pair('Players', lb.round === 'waiting' ? '3/40' : '14/40');
        pair('Round');
        lbRoundMeta(meta);
        lbEl(head, 'lobby__grow');
        lbBack(head);
    }

    function lbLink(parent, text, message) {
        const l = lbOn(lbEl(parent, 'lobby-info__link'), 'toast', message);
        lbText(l, text, 'lobby-info__link-label');
        lbIcon(l, 'external');
        return l;
    }

    function lbInfo(parent) {
        const info = lbEl(parent, 'lobby-info');
        info.id = 'server-info';
        const left = lbEl(info, 'lobby-info__left');
        lbEl(left, 'lobby-info__banner').style.backgroundImage = cssUrl('Banners/LobbyBannerBreach');
        lbText(left, 'Message of the day', 'lobby__eyebrow');
        lbText(lbEl(left, 'lobby-info__motd-scroll'), LB_MOTD, 'lobby-info__motd');

        const right = lbEl(info, 'lobby-info__right');
        lbText(right, 'Server', 'lobby__eyebrow');
        [['Map', 'Box Station'], ['Mode', 'Secret'], ['Build', 'v0.4.2 · bd2ba80'], ['Uptime', '2d 06:14']].forEach(([k, v]) => {
            const row = lbEl(right, 'lobby-info__kv');
            lbText(row, k, 'lobby-info__key');
            lbText(row, v, 'lobby-info__value');
        });
        const gap = lbEl(right, 'lobby-info__section-gap');
        const links = lbEl(gap, 'lobby-info__links');
        lbText(links, 'Change log', 'lobby__eyebrow');
        lbLink(links, 'All releases', 'In game: opens the releases page');
        const log = lbEl(gap, 'lobby-info__log');
        LB_CHANGES.forEach(([version, date, entries]) => {
            const group = lbEl(log, 'lobby-info__release');
            const hd = lbEl(group, 'lobby-info__release-head');
            lbText(hd, version, 'lobby-info__release-version');
            lbText(hd, date, 'lobby-info__release-date');
            lbLink(hd, 'Release notes', `In game: opens the ${version} release notes`);
            entries.forEach(([subject, ref]) => {
                const row = lbOn(lbEl(group, 'lobby-info__entry lobby-info__entry--link'), 'toast', `In game: opens ${ref} on GitHub`);
                lbEl(row, 'lobby-info__bullet');
                lbText(row, subject, 'lobby-info__entry-text');
                lbText(row, ref, 'lobby-info__entry-ref');
            });
        });
    }

    function lbChat(parent) {
        const chat = lbEl(parent, 'lobby-chat');
        chat.id = 'chat';
        const log = lbEl(chat, 'lobby-chat__log');
        log.id = 'chat-log';
        lb.chat.forEach(m => {
            const row = lbEl(log, 'lobby-chat__line');
            lbText(row, m.t, 'lobby-chat__time');
            lbText(row, m.ch.toUpperCase(), 'lobby-chat__channel lobby-chat__channel--' + m.ch);
            const body = lbEl(row, 'lobby-chat__body');
            if (m.sys) {
                lbText(body, m.sys, 'lobby-chat__system');
                return;
            }
            lbText(body, m.who + ':', 'lobby-chat__name' + (m.admin ? ' lobby-chat__name--admin' : ''));
            lbText(body, m.text, 'lobby-chat__text');
        });
        const stacked = lbStageLayout();
        const composer = lbEl(chat, 'lobby-chat__composer lobby-chat__composer--' + lb.channel + (stacked ? ' lobby-chat__composer--stacked' : ''));
        const channels = stacked ? lbEl(composer, 'lobby-chat__composer-row') : composer;
        [['ooc', 'OOC'], ['looc', 'LOOC'], ['admin', 'Admin']].forEach(([id, label]) =>
            lbOn(lbText(channels, label, `lobby-chat__channel-button lobby-chat__channel-button--${id}` + (lb.channel === id ? ' lobby-chat__channel-button--active' : '')), 'channel', id));
        const line = stacked ? lbEl(composer, 'lobby-chat__composer-row lobby-chat__composer-row--field') : composer;
        const input = h('input', 'lobby-chat__input');
        input.value = lb.draft;
        input.spellcheck = false;
        input.dataset.field = 'chat';
        input.placeholder = { ooc: 'Say something out of character', looc: 'Say something to players near your character', admin: 'Message the other admins' }[lb.channel];
        line.append(input);
        const send = lbOn(lbEl(line, 'lobby-chat__send'), 'send');
        send.title = 'Send (Enter)';
        lbIcon(send, 'send');
        lbText(send, 'Send', 'lobby-chat__send-label');
        lbText(chat, 'Enter sends to the selected channel. LOOC reaches players near your character.', 'lobby-chat__hint');
    }

    function lbLast(parent) {
        const root = lbEl(parent, 'round-end round-end--tab ' + (lb.noLast ? 'round-end--none' : 'round-end--ops'));
        root.id = 'last-round';
        const head = lbEl(root, 'round-end__header');
        lbText(head, 'Last round', 'lobby__eyebrow');
        lbText(head, 'Round 13 · 52 min', '');
        lbEl(head, 'round-end__spacer');
        lbText(head, 'Kept until the next round ends.', 'round-end__tab-note');

        const result = lbEl(root, 'round-end__result');
        result.id = 'round-end-result';
        lbText(result, 'Round result', 'lobby__eyebrow');
        const rr = lbEl(result, 'round-end__result-row');
        lbText(rr, 'Operatives win', 'round-end__title');
        lbText(rr, 'Station destroyed', 'round-end__destroyed');
        lbText(result, 'The nuclear device went off in the vault, three minutes before the shuttle docked.', 'round-end__reason');

        const facts = lbEl(root, 'round-end__facts');
        const fact = (label, divided) => {
            const f = lbEl(facts, 'round-end__fact' + (divided ? ' round-end__fact--divided' : ''));
            lbText(f, label, 'lobby__eyebrow');
            return lbEl(f, 'round-end__fact-value');
        };
        const mode = fact('Gamemode');
        lbText(mode, 'Secret', 'round-end__mode-configured round-end__struck');
        lbText(mode, '→', 'round-end__mode-configured');
        lbText(mode, 'Nuclear Emergency', 'round-end__mode round-end__ellipsis');
        const fate = fact('Your fate', true);
        lbText(fate, 'Died', 'round-end__fate round-end__dead');
        lbText(fate, 'Rory Lindqvist, Security Officer', 'round-end__fate-who round-end__ellipsis');

        const row = (parent, r, half) => {
            const dept = half ? ' round-end__row--antag' : ' lb-dept--' + LB_JOBS[r.job];
            const e = lbEl(parent, 'round-end__row' + dept + (half ? ' round-end__row--half' : '') + (r.you ? ' round-end__row--you' : ''));
            lbText(e, r.name, 'round-end__row-name round-end__ellipsis' + (r.alive ? '' : ' round-end__row-name--dead'));
            if (r.you) lbText(e, 'You', 'round-end__you');
            if (!half) lbText(e, r.job, 'round-end__row-job round-end__ellipsis lb-dept-text');
            lbText(e, r.alive ? 'Alive' : 'Dead', 'round-end__row-status ' + (r.alive ? 'round-end__alive' : 'round-end__dead'));
        };
        const ops = lbEl(root, 'round-end__section');
        ops.id = 'round-end-antagonists';
        const oh = lbEl(ops, 'round-end__section-head');
        lbText(oh, 'Operatives', 'round-end__section-label round-end__section-label--antag');
        lbText(oh, '3', '');
        const obj = lbEl(ops, 'round-end__objective');
        lbText(obj, 'Objective', 'round-end__objective-label');
        lbText(obj, 'Detonate the nuclear device on the station', 'round-end__objective-text');
        lbText(obj, 'Met', 'round-end__objective-result round-end__objective-result--met');
        const grid = lbEl(ops, 'round-end__ops-grid');
        [{ name: 'Vex Arden', alive: true }, { name: 'Kai Moreno', alive: true }, { name: 'Lu Petrov', alive: false }].forEach(r => row(grid, r, true));

        const crew = lbEl(root, 'round-end__crew');
        crew.id = 'round-end-crew';
        const ch = lbEl(crew, 'round-end__section-head');
        lbText(ch, 'Crew', 'round-end__section-label');
        lbText(ch, String(LB_LAST_CREW.length), '');
        const roster = lbEl(crew, 'round-end__roster');
        LB_LAST_CREW.forEach(r => row(roster, r, false));
    }

    // LobbyServerSettingsView's Refresh: the round's state in words, and which controls it offers.
    function lbAdmin(parent) {
        const scroll = lbEl(parent, 'lobby-admin__scroll');
        const form = lbEl(scroll, 'lobby-admin');
        form.id = 'server-settings';
        const idle = lbIdle();
        const warming = lb.round === 'starting';
        const card = lbEl(form, 'lobby-admin__card lb-tone--' + (lbRound().tone || 'neutral'));
        lbText(card, 'Round state', 'lobby__eyebrow');
        const body = lbEl(card, 'lobby-admin__card-body');
        const state = lbEl(body, '');
        const title = warming ? 'Starting in' : ({ loading: 'Loading station', ongoing: 'Round in progress', ending: 'Ending round' }[lb.round] || 'Waiting for players');
        let detail = 'Please wait…';
        if (warming) detail = lbClock(lb.secs);
        else if (lb.round === 'countdown') detail = 'Auto start in ' + lbClock(lb.secs);
        else if (lb.round === 'manual') detail = 'Manual start';
        else if (lb.round === 'held') detail = 'Auto start held. Start resumes it.';
        else if (idle) detail = 'Auto start at 4 connected';
        else if (lb.round === 'ongoing') detail = lbLong(lb.secs) + ' elapsed';
        else if (lb.round === 'ending') detail = lb.endHeld ? 'Summary held' : 'Lobby in ' + lbClock(lb.secs);
        lbText(state, title, 'lobby-admin__round-state');
        lbText(state, detail, 'lobby-admin__round-detail');
        const ready = lbEl(body, '');
        lbText(ready, 'Ready', 'lobby-admin__ready-label');
        lbText(ready, lb.round === 'waiting' ? '1/3' : `${lb.ready ? 7 : 6}/14`, 'lobby-admin__ready-ratio');

        lbText(form, 'Round controls', 'lobby-admin__section-title');
        const controls = lbEl(form, 'lobby-admin__controls');
        const button = (label, show, enabled, cls) => {
            if (!show) return;
            const b = lbText(controls, label, 'lobby-admin__button' + (cls ? ' ' + cls : '') + (enabled ? '' : ' lobby-admin__button--disabled'));
            if (enabled) lbOn(b, 'round-admin', label);
        };
        button('Start Round', idle, true, 'lobby-admin__button--start');
        button('Force Start', idle || warming, warming, 'lobby-admin__button--warn');
        button('Extend Timer', idle || warming, warming);
        button('Cancel Countdown', idle || warming, warming || lb.round === 'countdown', 'lobby-admin__button--warn');
        button('Stop Round', lb.round === 'ongoing', true, 'lobby-admin__button--danger');
        button('Hold Summary', lb.round === 'ending' && !lb.endHeld, true, 'lobby-admin__button--warn');
        button('Resume Summary', lb.round === 'ending' && lb.endHeld, true);

        lbText(form, 'Server info', 'lobby-admin__section-title');
        lbField(form, 'Server Name', 'RE:SS3D Sandbox');
        lbField(form, 'Message of the Day', LB_MOTD, true);

        lbText(form, 'Map and mode', 'lobby-admin__section-title');
        lbSelect(form, 'Map', 'Box Station', ['Box Station', 'Delta Station', 'Untitled map']);
        if (lb.round === 'loading') lbText(form, 'Loading map: Box Station…', 'lobby-admin__map-load lobby-admin__map-load--loading');
        else lbText(form, 'Map ready: Box Station', 'lobby-admin__map-load lobby-admin__map-load--ready');
        lbSelect(form, 'Game Mode', 'Secret', ['Secret', 'Extended', 'Nuclear Emergency']);
        const numbers = lbEl(form, 'lobby-admin__numbers');
        [['Player Cap', 40], ['Round Length (min)', 60], ['Minimum Connected', 4], ['Countdown (s)', 180], ['Minimum Operatives', 2]].forEach(([label, v]) => {
            const col = lbEl(numbers, 'lobby-admin__number');
            lbText(col, label, 'lobby-admin__label');
            lbInput(col, label, String(v));
        });

        lbText(form, 'Round rules', 'lobby-admin__section-title');
        lbRule(form, 'Allow Late Join', 'lateJoin', lb.lateJoin, '');
        lbRule(form, 'Auto Start Rounds', 'autoStart', lb.autoStart, 'Counts down on its own at 4 connected');
        lbText(form, 'Saved. Players see changes at once.', 'lobby-admin__status');
    }

    function lbInput(parent, key, value, multiline) {
        const i = h(multiline ? 'textarea' : 'input', 'lobby-admin__input');
        i.value = lb.fields[key] != null ? lb.fields[key] : value;
        i.spellcheck = false;
        i.dataset.field = 'admin:' + key;
        parent.append(i);
        return i;
    }

    function lbField(parent, label, value, multiline) {
        const f = lbEl(parent, 'lobby-admin__text' + (multiline ? ' lobby-admin__text--multiline' : ''));
        lbText(f, label, 'lobby-admin__label');
        lbInput(f, label, value, multiline);
    }

    function lbSelect(parent, label, value, items) {
        const f = lbEl(parent, 'lobby-admin__select');
        lbText(f, label, 'lobby-admin__label');
        const dd = lbEl(f, 'menu-dropdown');
        lbText(dd, lb.fields[label] || value, 'menu-dropdown__value');
        lbEl(dd, 'menu-dropdown__arrow');
        const list = lbEl(dd, 'menu-dropdown__list');
        items.forEach(v => lbText(list, v, 'menu-dropdown__item'));
        dd.dataset.select = label;
    }

    function lbRule(parent, label, key, on, reason) {
        const t = lbOn(lbEl(parent, 'lobby-admin__toggle' + (on ? ' lobby-admin__toggle--on' : '')), 'rule', key);
        if (reason) t.title = reason;
        lbIcon(lbEl(t, 'lobby-admin__checkmark'), 'tick');
        lbText(t, label, 'lobby-admin__toggle-text');
        if (reason) lbText(t, reason, 'lobby-admin__toggle-reason');
    }

    function lbJoin(root) {
        const overlay = lbEl(root, 'lobby-join lobby-join--open');
        overlay.id = 'lobby-join';
        const card = lbEl(overlay, 'lobby-join__card');
        const head = lbEl(card, 'lobby-join__header');
        lbText(head, 'Join the round', 'lobby-join__title');
        lbText(head, 'Round time', 'lobby-join__meta-key');
        lbText(head, lbLong(lb.secs), 'lobby-join__clock').dataset.lbClock = '';
        lbEl(head, 'lobby__grow');
        lbText(head, 'Esc', 'lobby__keycap');
        const close = lbOn(lbEl(head, 'lobby-join__close'), 'join-close');
        close.title = 'Close (Esc)';
        lbIcon(close, 'x');

        const body = lbEl(card, 'lobby-join__body');
        const jobs = lbEl(body, 'lobby-join__jobs');
        const open = lb.onlyFallback ? LB_OPEN.filter(([job]) => job === 'Assistant') : LB_OPEN.filter(([job]) => job !== lb.filled);
        const sh = lbEl(jobs, 'lobby-join__section-head');
        lbText(sh, 'Open jobs', 'lobby-join__section-title');
        lbText(sh, open.length + ' open', 'lobby-join__section-count');
        lbEl(sh, 'lobby__grow');
        lbText(sh, 'Pick a job to see what it does', 'lobby-join__hint');
        if (lb.filled || lb.onlyFallback) {
            const n = lbEl(jobs, 'lobby-join__notice' + (lb.filled ? ' lobby-join__notice--warning' : ''));
            lbIcon(n, lb.filled ? 'warning' : 'info');
            lbText(n, lb.filled ? `${lb.filled} filled up while you were choosing. Pick another job.`
                : 'Every other job is full right now. Assistant is always open, or you can observe instead.', 'lobby-join__notice-text');
        }
        const list = lbEl(jobs, 'lobby-join__job-list');
        open.forEach(([job, count, description]) => {
            const dept = LB_JOBS[job];
            const on = lb.picked === job;
            const item = lbEl(list, 'lobby-join__job-item lb-dept--' + dept + (on ? ' lobby-join__job-item--on' : ''));
            const row = lbOn(lbEl(item, 'lobby-join__job'), 'pick', job);
            lbEl(lbEl(row, 'lobby-join__ring'), 'lobby-join__ring-dot');
            lbIcon(lbEl(row, 'lobby-join__job-frame lb-dept--' + dept), LB_DEPT_ICON[dept], 'lb-tint');
            const text = lbEl(row, 'lobby-join__job-text');
            lbText(text, job, 'lobby-join__job-name');
            lbText(text, lbCap(dept), 'lobby-join__job-dept lb-dept-text lb-dept--' + dept);
            lbEl(row, 'lobby__grow');
            if (count) {
                lbText(row, count, 'lobby-join__job-count');
                lbText(row, 'open', 'lobby-join__job-count-word');
            } else {
                lbText(row, 'unlimited', 'lobby-join__job-count-word');
            }
            if (on) lbText(item, description, 'lobby-join__description');
        });
        const as = lbEl(jobs, 'lobby-join__as');
        lbIcon(lbEl(as, 'lobby-join__portrait-frame'), 'user', 'lobby-join__portrait');
        const at = lbEl(as, 'lobby-join__as-text');
        lbText(at, 'Joining as', 'lobby__eyebrow');
        lbText(at, lb.chars[lb.selected].name, 'lobby-join__character');
        const change = lbOn(lbEl(as, 'lobby-join__change'), 'switcher');
        change.id = 'lobby-join-change';
        change.title = 'Join as another of your characters';
        lbIcon(change, 'users');
        lbText(change, 'Change', 'lobby-join__change-label');
        const ok = lbEl(jobs, 'lobby-join__confirm' + (lb.picked ? ' lobby-join__confirm--dept lb-dept--' + LB_JOBS[lb.picked] : ' lobby-join__confirm--disabled'));
        if (lb.picked) lbOn(ok, 'join-confirm');
        const ct = lbEl(ok, 'lobby-join__confirm-text');
        lbText(ct, lb.picked ? 'Join as ' + lb.picked : 'Join', 'lobby-join__confirm-title');
        lbText(ct, 'Spawns you on the station straight away', 'lobby-join__confirm-subtitle');
        lbText(ok, 'Enter', 'lobby__keycap');

        // The manifest: who is aboard, by department; an open job nobody holds shows as a gap.
        const man = lbEl(body, 'lobby-join__manifest-section');
        const mh = lbEl(man, 'lobby-join__section-head');
        lbText(mh, 'Crew manifest', 'lobby-join__section-title');
        lbText(mh, LB_CREW.length + ' aboard', 'lobby-join__section-count');
        const grid = lbEl(lbEl(man, 'lobby-join__manifest-scroll'), 'lobby-join__manifest');
        const cols = [lbEl(grid, 'lobby-join__column lobby-join__column--left'), lbEl(grid, 'lobby-join__column')];
        LB_DEPTS.forEach((dept, i) => {
            const crew = LB_CREW.filter(([, job]) => LB_JOBS[job] === dept);
            const gaps = open.filter(([job]) => LB_JOBS[job] === dept && !LB_CREW.some(([, j]) => j === job));
            if (!crew.length && !gaps.length) return;
            const g = lbEl(cols[i < 4 ? 0 : 1], 'lobby-join__group lb-dept--' + dept);
            const gh = lbEl(g, 'lobby-join__group-head');
            lbIcon(gh, LB_DEPT_ICON[dept], 'lobby-join__group-icon lb-tint');
            lbText(gh, lbCap(dept), 'lobby-join__group-name');
            lbEl(gh, 'lobby__grow');
            lbText(gh, String(crew.length), 'lobby-join__group-count');
            crew.forEach(([name, job]) => {
                const r = lbEl(g, 'lobby-join__crew');
                lbText(r, name, 'lobby-join__crew-name');
                lbText(r, job, 'lobby-join__crew-job');
            });
            gaps.forEach(([job]) => lbText(g, `No ${job} aboard`, 'lobby-join__crew-missing'));
        });
    }

    function lbSwitcher(root) {
        const sw = lbEl(root, 'switcher switcher--open');
        sw.id = 'switcher';
        const head = lbEl(sw, 'switcher__head');
        lbText(head, 'Characters', 'switcher__title');
        lbText(head, `${lb.chars.length}/10`, 'switcher__count');
        lbEl(head, 'lobby__grow');
        lbText(head, 'Pick who you play', 'switcher__hint');
        const list = lbEl(sw, 'switcher__list');
        const full = lb.chars.length >= 10;
        lb.chars.forEach((c, i) => {
            const selected = i === lb.selected;
            const dept = c.high ? ' switcher__row--dept lb-dept--' + LB_JOBS[c.high] : '';
            const row = lbOn(lbEl(list, 'switcher__row' + dept + (selected ? ' switcher__row--selected' : '')), 'select-char', i);
            lbIcon(lbEl(row, 'switcher__portrait'), 'user', 'switcher__silhouette');
            const text = lbEl(row, 'switcher__text');
            lbText(text, c.name, 'switcher__name');
            const high = lbEl(text, 'switcher__high');
            if (c.high) {
                lbEl(high, 'lb-swatch lb-dept--' + LB_JOBS[c.high]);
                lbText(high, 'High:', 'switcher__high-prefix');
                lbText(high, c.high, 'switcher__high-job lb-dept-text');
            } else {
                lbText(high, 'No job preferences', 'switcher__high-prefix');
            }
            if (selected) {
                const s = lbEl(row, 'switcher__selected');
                lbIcon(s, 'check');
                lbText(s, 'Selected', 'switcher__selected-label');
            }
            const dup = lbEl(row, 'switcher__action' + (full ? ' switcher__action--disabled' : ''));
            dup.title = 'Duplicate';
            if (!full) lbOn(dup, 'duplicate', i);
            lbIcon(dup, 'duplicate');
            const only = lb.chars.length <= 1;
            const del = lbEl(row, 'switcher__action' + (only ? ' switcher__action--disabled' : ''));
            del.title = 'Delete';
            if (!only) lbOn(del, 'delete', i);
            lbIcon(del, 'trash');
            if (lb.confirmDelete === i) {
                const cf = lbEl(list, 'switcher__confirm');
                lbText(cf, `Delete ${c.name}? Their look, jobs and antag choices go with them. This can't be undone.`, 'switcher__confirm-text');
                const bs = lbEl(cf, 'switcher__confirm-buttons');
                const yes = lbOn(lbEl(bs, 'switcher__confirm-button switcher__confirm-button--danger'), 'delete-yes', i);
                lbIcon(yes, 'trash');
                lbText(yes, 'Delete', 'switcher__confirm-label');
                lbText(lbOn(lbEl(bs, 'switcher__confirm-button'), 'delete-no'), 'Keep', 'switcher__confirm-label');
            }
        });
        const foot = lbEl(sw, 'switcher__foot');
        const add = lbEl(foot, 'switcher__new' + (full ? ' switcher__new--disabled' : ''));
        if (!full) lbOn(add, 'new-char');
        lbIcon(add, 'plus');
        lbText(add, 'New character', 'switcher__new-label');
        lbText(foot, 'Ten characters is the most you can keep. Delete one to make room.', 'switcher__full' + (full ? '' : ' switcher__full--hidden'));
        return sw;
    }

    // Beside the button that opened it, vertically centred on it and kept on screen (LobbyView places it in C#).
    function lbPlaceSwitcher(root, sw) {
        const anchor = $(lb.join ? '#lobby-join-change' : '#lobby-switch-character', root);
        if (!anchor) return;
        const box = root.getBoundingClientRect();
        const scale = box.width / root.offsetWidth || 1;
        const a = anchor.getBoundingClientRect();
        if (lbStageLayout() && !lb.join) {
            const centre = (a.left + a.width / 2 - box.left) / scale;
            sw.style.left = Math.max(16, Math.min(root.offsetWidth - sw.offsetWidth - 16, centre - sw.offsetWidth / 2)) + 'px';
            sw.style.top = Math.max(16, (a.top - box.top) / scale - sw.offsetHeight - 14) + 'px';
            return;
        }
        const left = (a.right - box.left) / scale + 14;
        const mid = (a.top + a.height / 2 - box.top) / scale;
        const top = Math.max(16, Math.min(root.offsetHeight - sw.offsetHeight - 16, mid - 70));
        sw.style.left = left + 'px';
        sw.style.top = top + 'px';
    }

    function lbPrompt(root) {
        const p = lbEl(root, 'lobby-prompt');
        p.id = 'map-stop-prompt';
        const card = lbEl(p, 'lobby-prompt__card');
        lbText(card, 'Unsaved map changes', 'lobby-prompt__title');
        lbText(card, 'This new map has unsaved changes. Stopping the round unloads the station. Save and stop saves it under a new name first; Discard loses these edits.', 'lobby-prompt__text');
        const f = lbEl(card, 'lobby-admin__text');
        lbText(f, 'New map name', 'lobby-admin__label');
        const i = h('input', 'lobby-admin__input');
        i.value = lb.mapName;
        i.spellcheck = false;
        i.dataset.field = 'mapName';
        f.append(i);
        const named = lb.mapName.trim() !== '';
        if (!named) lbText(card, 'Give the map a name to save it.', 'lobby-prompt__error');
        const bs = lbEl(card, 'lobby-prompt__buttons');
        lbOn(lbText(bs, 'Cancel', 'lobby-admin__button'), 'prompt', 'cancel');
        lbOn(lbText(bs, 'Discard and stop', 'lobby-admin__button lobby-admin__button--danger'), 'prompt', 'discard');
        const save = lbText(bs, 'Save and stop', 'lobby-admin__button lobby-admin__button--start' + (named ? '' : ' lobby-admin__button--disabled'));
        if (named) lbOn(save, 'prompt', 'save');
    }

    function lbRender() {
        const cv = $('#lb-canvas');
        if (!cv) return;
        const focus = document.activeElement && cv.contains(document.activeElement) ? document.activeElement : null;
        const field = focus && focus.dataset.field;
        const caret = focus && focus.selectionStart;
        let root = $('.lobby', cv);
        if (!root) {
            root = h('div', 'lobby');
            const station = h('div', 'lobby__station');
            station.style.backgroundImage = cssUrl('Renders/PnCampfire');
            root.append(station, h('div', 'lobby__veil'));
            cv.append(root);
        }
        $$(':scope > :not(.lobby__station):not(.lobby__veil)', root).forEach(e => e.remove());
        root.classList.toggle('lobby--stage', lbStageLayout());
        lbSidebar(root);
        if (lbStageLayout()) lbStage(root);
        lbMain(root);
        if (lb.join) lbJoin(root);
        if (lb.switcher) lbPlaceSwitcher(root, lbSwitcher(root));
        if (lb.prompt) lbPrompt(root);
        $$('[data-lb-chip]').forEach(c => c.classList.toggle('is-on', c.dataset.lbChip === lb.chip));
        $$('[data-lb-layout]').forEach(c => c.classList.toggle('is-on', c.dataset.lbLayout === lb.layout));
        const log = $('#chat-log', root);
        if (log) log.scrollTop = log.scrollHeight;
        if (field) {
            const again = $(`[data-field="${field}"]`, cv);
            if (again) {
                again.focus();
                if (caret != null) again.setSelectionRange(caret, caret);
            }
        }
    }

    // The shipped lobby's capture nearest each state (the local session showed "waiting for an admin", no countdown).
    const LB_OLD = {
        'pre-round': 'pre-round', 'ready': 'ready', 'waiting for players': 'pre-round', 'countdown held': 'pre-round',
        'loading': 'pre-round', 'starting': 'in-progress', 'in progress': 'in-progress', 'late join off': 'in-progress',
        'escape menu': 'in-progress', 'round over': 'round-over', 'join': 'join', 'join: job filled': 'join',
        'join: only assistant': 'join', 'switch character': 'switcher', 'delete character': 'switcher', 'chat': 'chat',
        'settings': 'settings', 'last round': 'round-over', 'server settings': 'server-settings', 'unsaved map': 'server-settings',
    };

    function lbState(name) {
        Object.assign(lb, LB_BASE, LB_STATES[name], { chip: name, chars: LB_CHARS.map(c => ({ ...c })), selected: 0, fields: {} });
        lb.secs = LB_SECS[lb.round] || 0;
        lbRender();
        syncOld('#lb-stage', 'lobby-' + LB_OLD[name]);
    }

    function lbSetRound(round) {
        lb.round = round;
        lb.secs = LB_SECS[round] || 0;
    }

    function lbSend() {
        const text = lb.draft.trim();
        if (!text) return;
        const now = new Date();
        const t = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;
        lb.chat.push({ t, ch: lb.channel, who: mm.name.trim() || 'Spessman', admin: true, text });
        lb.draft = '';
    }

    function lbDo(action, arg) {
        lb.chip = null;
        const c = lb.chars[lb.selected];
        switch (action) {
            case 'ready':
                lb.ready = !lb.ready;
                break;
            case 'join':
                Object.assign(lb, { join: true, picked: null, filled: null, onlyFallback: false, switcher: false });
                break;
            case 'join-close':
                Object.assign(lb, { join: false, switcher: false });
                break;
            case 'pick':
                lb.picked = arg;
                break;
            case 'join-confirm':
                toast(`In game: the lobby closes and ${c.name} spawns as ${lb.picked}`);
                Object.assign(lb, { join: false, switcher: false, picked: null, spawned: true });
                break;
            case 'observe':
                toast('In game: the lobby closes and you go in as a ghost');
                return;
            case 'card':
                toast('In game: the character window opens (the character setup tab)');
                return;
            case 'switcher':
                lb.switcher = !lb.switcher;
                lb.confirmDelete = null;
                break;
            case 'select-char':
                lb.selected = Number(arg);
                lb.switcher = false;
                break;
            case 'duplicate': {
                const i = Number(arg);
                lb.chars.splice(i + 1, 0, { ...lb.chars[i], name: lb.chars[i].name + ' (copy)' });
                if (i < lb.selected) lb.selected++;
                break;
            }
            case 'delete':
                lb.confirmDelete = Number(arg);
                break;
            case 'delete-no':
                lb.confirmDelete = null;
                break;
            case 'delete-yes': {
                const i = Number(arg);
                lb.chars.splice(i, 1);
                if (i < lb.selected || lb.selected >= lb.chars.length) lb.selected = Math.max(0, lb.selected - 1);
                lb.confirmDelete = null;
                break;
            }
            case 'new-char':
                lb.chars.push({ name: LB_NAMES[lb.chars.length % LB_NAMES.length], high: null });
                break;
            case 'music':
                lb.music = !lb.music;
                break;
            case 'next':
                lb.track = (lb.track + 1) % MM_TRACKS.length;
                lb.music = true;
                break;
            case 'bug':
                toast('In game: the bug report opens, with your log attached');
                return;
            case 'quit':
                clearTimeout(lbQuitTimer);
                if (lb.quitArmed) {
                    toast('In game: you leave the server, back to the main menu');
                    lb.quitArmed = false;
                } else {
                    lb.quitArmed = true;
                    lbQuitTimer = setTimeout(() => {
                        lb.quitArmed = false;
                        lbRender();
                    }, 3000);
                }
                break;
            case 'quit-disarm':
                clearTimeout(lbQuitTimer);
                lb.quitArmed = false;
                break;
            case 'back':
                toast('In game: the menu closes, back to your body');
                return;
            case 'tab':
                lb.tab = arg;
                break;
            case 'admin':
                lb.adminOn = !lb.adminOn;
                if (!lb.adminOn && lb.tab === 'admin') lb.tab = 'info';
                break;
            case 'channel':
                lb.channel = arg;
                break;
            case 'send':
                lbSend();
                break;
            case 'rule':
                if (arg === 'lateJoin') {
                    lb.lateJoin = !lb.lateJoin;
                } else {
                    lb.autoStart = !lb.autoStart;
                    if (lbIdle()) lbSetRound(lb.autoStart ? 'countdown' : 'manual');
                }
                break;
            case 'round-admin':
                if (arg === 'Start Round') lbSetRound('starting');
                else if (arg === 'Force Start') lbSetRound('ongoing');
                else if (arg === 'Extend Timer') lb.secs += 60;
                else if (arg === 'Cancel Countdown') lb.round === 'starting' ? lbSetRound('countdown') : lbSetRound('held');
                else if (arg === 'Stop Round') lb.prompt = true;
                else lb.endHeld = arg === 'Hold Summary';
                break;
            case 'prompt':
                lb.prompt = false;
                if (arg !== 'cancel') {
                    toast(arg === 'save' ? `In game: "${lb.mapName.trim()}" is saved, the station unloads` : 'In game: the edits are dropped, the station unloads');
                    Object.assign(lb, { spawned: false, ready: false });
                    lbSetRound('countdown');
                }
                break;
            case 'toast':
                toast(arg);
                return;
            default:
                return;
        }
        lbRender();
    }

    // The clock runs: a countdown to the start (then the warm-up, then the round), round time up, the summary down.
    function lbTick() {
        if (!$('#lb-canvas')) return;
        const r = lb.round;
        if (r === 'ongoing') {
            lb.secs++;
        } else if (r === 'countdown' || r === 'starting' || (r === 'ending' && !lb.endHeld)) {
            lb.secs--;
            if (lb.secs <= 0) {
                if (r === 'countdown') {
                    toast('In game: the station loads, then a short warm-up');
                    lbSetRound('starting');
                } else if (r === 'starting') {
                    toast(lb.ready ? 'In game: the round starts and you spawn' : 'In game: the round starts; you can still join');
                    if (lb.ready) lb.spawned = true;
                    lbSetRound('ongoing');
                } else {
                    toast('In game: everyone is back in the lobby');
                    Object.assign(lb, { spawned: false, ready: false });
                    lbSetRound('countdown');
                }
                lb.chip = null;
                lbRender();
                return;
            }
        } else {
            return;
        }
        $$('[data-lb-timer]').forEach(e => { e.textContent = r === 'ongoing' ? lbLong(lb.secs) : lbClock(lb.secs); });
        $$('[data-lb-clock]').forEach(e => { e.textContent = lbLong(lb.secs); });
    }

    // ══════════════════════════════════════════════════════════════════════
    // Character window: the shipped window's content in the character setup reference's look
    // ══════════════════════════════════════════════════════════════════════
    const CW_SKIN = ['#f6c6a6', '#f0d3bf', '#e2b48f', '#d6a079', '#b77a52', '#8f5a3c', '#6f432e', '#4a2c22'];
    const CW_EYES = ['#3a2a20', '#4d7fb0', '#4d8f5e', '#8a7d34', '#7d8a8a', '#b8862f', '#6a3f2a', '#2c2c30'];
    const CW_HAIR_COLOURS = ['#141414', '#3b2416', '#6e4126', '#8a3a1c', '#9a6a3e', '#b8923c', '#e3c77a', '#f1ead6', '#8d8d8d',
        '#f4f4f4', '#c0281c', '#e0601c', '#f2a183', '#ef8fb8', '#8a3ec6', '#2f5fe0', '#2f9e4a', '#1aa3a0'];
    // Names as the shipped window shows them, keyed to the baked pictures (Art/Icons/Lobby/Styles).
    const CW_STYLES = {
        hair: [['None', 'hair-none'], ['Anime', 'anime'], ['Beep', 'beep'], ['Fry', 'fry'], ['Afro', 'afro'], ['Afro 2', 'afro2'],
            ['Balding 1', 'balding1'], ['Balding 2', 'balding2'], ['Big CEO', 'bigceo'], ['Clown', 'clown'], ['Crew Cut', 'crewcut'],
            ['Dyhr', 'dyhr'], ['Emo', 'emo'], ['Fancy Kid', 'fancykid'], ['Grizzly Outlaw', 'grizzlyoutlaw'],
            ['Hipster Ponytail', 'hipsterponytail2'], ['Long Hair Long', 'longhairlong'], ['Long Hair Medium', 'longhairmedium'],
            ['Long Hair Short', 'longhairshort'], ['Mane', 'mane'], ['Mohawk', 'mohawk'], ['Mohawk Alt', 'mohawkalternative'],
            ['Mullet', 'mullet'], ['Poofy Bangs', 'poofybangsshort'], ['Short Hair', 'shorthair'], ['Short Messy', 'shortmessy'],
            ['Simple 1', 'simple1'], ['Simple 2', 'simple2'], ['Simple 3', 'simple3'], ['Simple Hime', 'simplehime']]
            .map(([name, key]) => [name, key === 'hair-none' ? key : 'hair-humanhair' + key]),
        beard: [['None', 'beard-none'], ['Danish Full', 'danishfull'], ['Amish', 'amish'], ['Cabron', 'cabron'], ['Chaplin', 'chaplin'],
            ['Dali', 'dali'], ['Danish Mustache', 'danishmustache'], ['Freeman', 'freeman'], ['Frenchie', 'frenchie'], ['Full', 'full'],
            ['Fuller', 'fuller'], ['Fu Manchu', 'fumanchu'], ['Goatee', 'goatee'], ['Mayor', 'mayor'], ['Mustache', 'mustache'],
            ['Mutton Chops', 'muttonchops'], ['Plumber', 'plumber'], ['Rancher', 'rancher'], ['Saxon', 'saxonthegnsstache'],
            ['Shadow', 'shadow'], ['Usual Mike', 'usualmike']]
            .map(([name, key]) => [name, key === 'beard-none' ? key : 'beard-humanfacehair' + key]),
        eyebrows: [['None', 'eyebrows-none'], ['Default', 'default'], ['Bold', 'bold'], ['Circle', 'circle'], ['Classic', 'classic'],
            ['Dainty', 'dainty']].map(([name, key]) => [name, key === 'eyebrows-none' ? key : 'eyebrows-humaneyebrows' + key]),
    };
    // Departments and jobs as the shipped Jobs tab lists them; null slots = not in the game yet.
    const CW_JOBS = [
        ['command', [['Captain', 'Captain', '1 slot'], ['Head of Personnel', null, null]]],
        ['science', [['Research Director', null, null], ['Scientist', null, null]]],
        ['security', [['Head of Security', null, null], ['Security', 'SecurityOfficer', '1 slot'], ['Detective', null, null]]],
        ['cargo', [['Quartermaster', null, null], ['Cargo Technician', 'CargoTechnician', null]]],
        ['engineering', [['Chief Engineer', 'ChiefEngineer', null], ['Engineer', 'Engineer', 'unlimited'], ['Atmospheric Technician', null, null]]],
        ['service', [['Bartender', 'Bartender', null], ['Chef', 'Chef', null], ['Janitor', 'Janitor', null]]],
        ['medical', [['Chief Medical Officer', null, null], ['Doctor', 'Doctor', 'unlimited'], ['Chemist', 'Chemist', null]]],
        ['civilian', [['Assistant', 'Assistant', 'unlimited']]],
    ];
    const CW_LEVELS = ['high', 'medium', 'low', 'never'];
    const CW_WIDE = ['jobs', 'antags'];
    // The shipped window's capture behind each tab of the design switch.
    const CW_OLD = { profile: 'appearance', body: 'appearance', skin: 'appearance', hair: 'appearance', beard: 'appearance', jobs: 'jobs', antags: 'antags' };
    const cw = {
        swatch: { skin: 1, eyes: 3, hair: 1 },
        style: { hair: 'Long Hair Medium', beard: 'None', eyebrows: 'Default' },
        jobs: {},
        dirty: false,
    };

    function cwMarkDirty(dirty = true) {
        cw.dirty = dirty;
        const save = $('[data-cw-save]');
        if (!save) return;
        save.classList.toggle('cw-save--dirty', dirty);
        $('[data-cw-state]', save).textContent = dirty ? 'unsaved changes' : 'saved';
        $$('.cw-small-button', save).forEach(b => b.classList.toggle('cw-small-button--disabled', !dirty));
    }

    function cwSwatches() {
        const sets = { skin: CW_SKIN, eyes: CW_EYES, hair: CW_HAIR_COLOURS };
        $$('[data-cw-swatches]').forEach(host => {
            const kind = host.dataset.cwSwatches;
            host.textContent = '';
            sets[kind].forEach((colour, i) => {
                const s = h('div', 'cw-swatch' + (cw.swatch[kind] === i ? ' cw-swatch--selected' : ''));
                s.style.backgroundColor = colour;
                s.dataset.cwSwatch = kind + ':' + i;
                host.append(s);
            });
        });
    }

    function cwStyles() {
        $$('[data-cw-styles]').forEach(host => {
            const kind = host.dataset.cwStyles;
            host.textContent = '';
            CW_STYLES[kind].forEach(([name, file]) => {
                const t = h('div', 'cw-style' + (cw.style[kind] === name ? ' cw-style--selected' : ''));
                t.style.backgroundImage = cssUrl('Styles/' + file);
                t.title = name;
                t.dataset.cwStyle = kind + ':' + name;
                host.append(t);
            });
            const label = $(`[data-cw-style-name="${kind}"]`);
            if (label) label.textContent = cw.style[kind].toLowerCase();
        });
    }

    function cwJobs() {
        const host = $('[data-cw-jobs]');
        if (!host) return;
        host.textContent = '';
        CW_JOBS.forEach(([dept, jobs]) => {
            const box = h('div', 'cw-dept lb-dept--' + dept);
            const head = h('div', 'cw-dept__head');
            const glyph = h('div', 'cw-dept__glyph');
            const u = cssUrl(LB_DEPT_ICON[dept]);
            glyph.style.webkitMaskImage = u;
            glyph.style.maskImage = u;
            head.append(glyph, h('div', 'cw-dept__name', { text: dept }));
            box.append(head);
            jobs.forEach(([name, icon, slots]) => {
                const open = slots != null;
                const row = h('div', 'cw-job' + (open ? '' : ' cw-job--locked'));
                const ic = h('div', 'cw-job__icon');
                if (icon) ic.style.backgroundImage = cssUrl('Jobs/' + icon);
                else {
                    ic.classList.add('cw-dept__glyph');
                    ic.style.webkitMaskImage = u;
                    ic.style.maskImage = u;
                }
                row.append(ic, h('div', 'cw-job__name', { text: name }));
                if (open) {
                    row.append(h('div', 'cw-job__slots', { text: slots }));
                    const seg = h('div', 'menu-segmented cw-seg');
                    const level = cw.jobs[name] || 'never';
                    CW_LEVELS.forEach(l => {
                        const o = h('div', 'menu-segmented__option cw-level--' + l + (l === level ? ' menu-segmented__option--selected' : ''),
                            { text: l === 'medium' ? 'med' : l });
                        o.dataset.cwJob = name + ':' + l;
                        seg.append(o);
                    });
                    row.append(seg);
                } else {
                    row.append(h('div', 'cw-job__locked', { text: 'not in the game yet' }));
                }
                box.append(row);
            });
            host.append(box);
        });
    }

    function cwPicker() {
        const list = $('[data-cw-chars]');
        if (!list) return;
        ['Omar Maddox', 'Ada Brandt', 'Mags Okafor'].forEach(n => list.append(h('div', 'menu-dropdown__item', { text: n })));
        list.append(h('div', 'menu-dropdown__item', { text: '+ new character' }));
    }

    function cwSetHigh(name, level) {
        // One job can be High: setting another demotes the old one to Medium, as the shipped window does.
        if (level === 'high') Object.keys(cw.jobs).forEach(k => { if (cw.jobs[k] === 'high' && k !== name) cw.jobs[k] = 'medium'; });
        cw.jobs[name] = level;
        const high = Object.keys(cw.jobs).find(k => cw.jobs[k] === 'high');
        const outfit = $('[data-cw-outfit] .menu-dropdown__value');
        if (outfit && outfit.textContent.startsWith('high job')) outfit.textContent = `high job (${high ? high.toLowerCase() : 'none: assistant'})`;
        cwJobs();
    }

    function buildCharwin() {
        const cv = $('#cw-canvas');
        if (!cv) return;
        $('[data-cw-model]', cv).style.backgroundImage = 'url(renders/lobby-character.png)';
        cwSwatches();
        cwStyles();
        cwJobs();
        cwPicker();
        cwMarkDirty(false);

        cv.addEventListener('click', e => {
            const tab = e.target.closest('[data-cw-tabs] > .menu-tab');
            if (tab) {
                cv.classList.toggle('cw--wide', CW_WIDE.includes(tab.dataset.tab));
                syncOld('#cw-stage', 'charwin-' + CW_OLD[tab.dataset.tab]);
                return;
            }
            const sw = e.target.closest('[data-cw-swatch]');
            if (sw) {
                const [kind, i] = sw.dataset.cwSwatch.split(':');
                cw.swatch[kind] = Number(i);
                cwSwatches();
                cwMarkDirty();
                return;
            }
            const st = e.target.closest('[data-cw-style]');
            if (st) {
                const [kind, name] = st.dataset.cwStyle.split(':');
                cw.style[kind] = name;
                cwStyles();
                cwMarkDirty();
                return;
            }
            const job = e.target.closest('[data-cw-job]');
            if (job) {
                const [name, level] = job.dataset.cwJob.split(':');
                cwSetHigh(name, level);
                cwMarkDirty();
                return;
            }
            const picked = e.target.closest('[data-cw-chars] > .menu-dropdown__item');
            if (picked) {
                toast(picked.textContent.startsWith('+') ? 'In game: a new random character, saved at once'
                    : `In game: switches to ${picked.textContent}${cw.dirty ? ', asking to save or discard first' : ''}`);
                return;
            }
            if (e.target.closest('[data-cw-angle] .menu-segmented__option')) {
                toast('In game: the booth turns the character to that side; dragging turns it freely');
                return;
            }
            if (e.target.closest('[data-cw-antag] .menu-segmented__option, [data-cw-fallback] .menu-segmented__option, [data-cw-outfit] .menu-dropdown__item')) {
                cwMarkDirty();
                return;
            }
            const act = e.target.closest('[data-cw]');
            if (!act) return;
            switch (act.dataset.cw) {
                case 'random-name': {
                    const input = $('[data-cw-name]', cv);
                    input.value = LB_NAMES[Math.floor(Math.random() * LB_NAMES.length)];
                    cwMarkDirty();
                    break;
                }
                case 'random-look':
                    cw.swatch.skin = Math.floor(Math.random() * CW_SKIN.length);
                    cw.swatch.eyes = Math.floor(Math.random() * CW_EYES.length);
                    cw.swatch.hair = Math.floor(Math.random() * CW_HAIR_COLOURS.length);
                    ['hair', 'beard', 'eyebrows'].forEach(k => { cw.style[k] = CW_STYLES[k][Math.floor(Math.random() * CW_STYLES[k].length)][0]; });
                    cwSwatches();
                    cwStyles();
                    cwMarkDirty();
                    toast('In game: body, colours and styles roll; the name and jobs stay');
                    break;
                case 'save':
                    if (cw.dirty) toast('In game: saved to your profile');
                    cwMarkDirty(false);
                    break;
                case 'discard':
                    if (cw.dirty) toast('In game: back to the last save');
                    cwMarkDirty(false);
                    break;
                case 'lobby':
                    toast(cw.dirty ? 'In game: asks to save or discard, then back to the lobby' : 'In game: back to the lobby');
                    break;
                default:
                    break;
            }
        });
        cv.addEventListener('input', e => { if (e.target.matches('[data-cw-name]')) cwMarkDirty(); });
        cv.addEventListener('pointerup', e => { if (e.target.closest('.menu-slider')) cwMarkDirty(); });
        $('[data-cw-viewport]', cv).addEventListener('pointerdown', () => toast('In game: drag turns the character (the lobby stage does the same)'));
    }

    function buildLobby() {
        const cv = $('#lb-canvas');
        if (!cv) return;
        const chips = $('[data-lb-chips]');
        Object.keys(LB_STATES).forEach(name => {
            const b = h('button', 'chip-btn', { text: name });
            b.dataset.lbChip = name;
            b.addEventListener('click', () => lbState(name));
            chips.append(b);
        });
        lb.layout = store.get('lobby-layout') === 'a' ? 'a' : 'b';
        $$('[data-lb-layout]').forEach(b => b.addEventListener('click', () => {
            lb.layout = b.dataset.lbLayout;
            store.set('lobby-layout', lb.layout);
            lbRender();
        }));
        lb.chat = LB_CHAT.slice();
        cv.addEventListener('click', e => {
            const t = e.target.closest('[data-lb], [data-mm]');
            if (!t || !cv.contains(t)) return;
            if (t.dataset.lb) {
                lbDo(t.dataset.lb, t.dataset.arg);
            } else if (settingsAct(t.dataset.mm, t.dataset.arg)) {
                lb.chip = null;
                lbRender();
            }
        });
        cv.addEventListener('input', e => {
            const f = e.target.dataset.field;
            if (!f) return;
            if (f === 'chat') lb.draft = e.target.value;
            else if (f.startsWith('admin:')) lb.fields[f.slice(6)] = e.target.value;
            else if (f === 'mapName') {
                lb.mapName = e.target.value;
                lbRender();
            }
        });
        cv.addEventListener('keydown', e => {
            if (e.key === 'Enter' && e.target.dataset.field === 'chat') lbDo('send');
        });
        cv.addEventListener('pointerdown', e => dragMenuSlider(e, (key, v) => {
            if (key === 'volume') lb.volume = v;
            else settingFromSlider(cv, key, v);
        }, key => {
            if (key !== 'volume') lbRender(); // the changed-from-default dot
        }));
        // Dropdowns are wired page-wide (wireMenus); keep their picks in the state.
        cv.addEventListener('click', e => {
            const item = e.target.closest('.menu-dropdown__item');
            if (!item) return;
            if (item.closest('.settings-panel__select')) mm.settings.resolution = item.textContent;
            const dd = item.closest('[data-select]');
            if (dd) lb.fields[dd.dataset.select] = item.textContent;
        });

        // Esc closes the top thing while the pointer is over the screen, as in game.
        let over = false;
        cv.addEventListener('pointerenter', () => { over = true; });
        cv.addEventListener('pointerleave', () => { over = false; });
        document.addEventListener('keydown', e => {
            if (!over || e.key !== 'Escape') return;
            if (mm.listening && lb.tab === 'settings') {
                settingsAct('listen', mm.listening);
                lbRender();
            }
            else if (lb.prompt) lbDo('prompt', 'cancel');
            else if (lb.switcher) lbDo('switcher');
            else if (lb.join) lbDo('join-close');
            else if (lb.quitArmed) lbDo('quit-disarm');
            else if (lb.spawned) lbDo('back');
        });
        setInterval(lbTick, 1000);
        lbState('pre-round');
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
        step('main menu', buildMainMenu);
        step('lobby', buildLobby);
        step('charwin', buildCharwin);
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
