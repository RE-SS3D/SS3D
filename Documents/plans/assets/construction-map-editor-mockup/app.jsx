const { Button, ToggleButton, Window, Badge } = window.SS3DDesignSystem_41b9f4;
const { useState, useEffect, useRef } = React;

/* ---------------------------------------------------------------------- */
/* Data                                                                    */
/* ---------------------------------------------------------------------- */

const MODES = [
{
  key: "upper", label: "Upper", icon: "chevronUp",
  subcats: [
  { key: "flooring", label: "Flooring", icon: "grid", items: ["Steel Plating", "Grating", "Carpet Tile", "Catwalk", "Reinforced Floor", "Freezer Floor", "Wood Paneling", "Plating (Damaged)"] },
  { key: "turfs", label: "Turfs", icon: "terrain", items: ["Asteroid Rock", "Snow", "Grass Turf", "Sand", "Lava Rock", "Ice Sheet"] },
  { key: "walls", label: "Walls", icon: "bracket", items: ["Reinforced Wall", "Plasteel Wall", "Girder", "Wood Wall", "Wall Girder", "Rock Wall"] },
  { key: "doors", label: "Doors", icon: "door", items: ["Airlock", "Blast Door", "Firelock", "Windoor", "Shuttle Door", "Maintenance Hatch"] },
  { key: "tileObjects", label: "Tile Objects", icon: "cube", items: ["Table", "Chair", "Locker", "Crate", "Vending Machine", "APC", "Camera Mount"] },
  { key: "wallAttachments", label: "Wall Attachments", icon: "bracket", items: ["Wall Light", "Sign", "Fire Alarm", "Intercom", "Air Vent", "Power Conduit"] }]

},
{
  key: "lower", label: "Lower", icon: "chevronDown",
  subcats: [
  { key: "piping", label: "Piping", icon: "pipe", items: ["Pipe (Straight)", "Pipe (Bend)", "Pump", "Valve", "Manifold", "Filter Unit"] },
  { key: "disposals", label: "Disposals", icon: "trash", items: ["Disposal Unit", "Chute Pipe", "Bin", "Outlet"] },
  { key: "baseTiles", label: "Base Tiles", icon: "dots9", items: ["Lattice", "Reinforced Wall", "Girder", "Plasteel Wall", "Hull Plating"] }]

},
{
  key: "items", label: "Items", icon: "box",
  subcats: [
  { key: "foodDrink", label: "Food & Drink", icon: "box", items: ["Banana", "Soda Can", "Donut", "Coffee Cup", "Candy Bar", "Water Bottle"] },
  { key: "tools", label: "Tools", icon: "settings", items: ["Toolbox", "Crowbar", "Wrench", "Welding Tool", "Multitool", "Screwdriver"] },
  { key: "medical", label: "Medical", icon: "cube", items: ["Health Analyzer", "Medkit", "Bruise Pack", "Ointment", "Pill Bottle", "Defibrillator"] },
  { key: "security", label: "Security", icon: "bolt", items: ["Stun Baton", "Handcuffs", "Flashlight", "Riot Shield", "Pepper Spray"] },
  { key: "misc", label: "Misc", icon: "dots9", items: ["Janicart", "Mop", "Fire Extinguisher", "Bucket", "Cardboard Box"] }]

},
{
  key: "scripting", label: "Scripting & Placements", icon: "script",
  subcats: [
  { key: "atmospherics", label: "Atmospherics", icon: "pipe", items: ["Air Mix", "Plasma", "CO2", "Nitrogen", "Oxygen", "Nitrous Oxide"] },
  { key: "spawnPlacements", label: "Spawn Placements", icon: "pin", items: ["Crew Spawn", "Antag Spawn", "Latejoin Point", "Cryo Pod"] },
  { key: "randomSpawners", label: "Random Item Spawners", icon: "dice", items: ["Loot Spawner", "Weapon Spawner", "Random Junk", "Vending Restock"] },
  { key: "triggers", label: "Triggers", icon: "bolt", items: ["Proximity Trigger", "Timer Trigger", "Switch Trigger", "Damage Trigger"] }]

}];


const SWATCH_TINTS = ["var(--ramp-rust-300)", "var(--ramp-blue-400)", "var(--ramp-steel-300)", "var(--ramp-amber-400)", "var(--ramp-green-400)"];

/* ---------------------------------------------------------------------- */
/* Small shared bits                                                       */
/* ---------------------------------------------------------------------- */

function IconTab({ icon, label, active, onClick, vertical, size = 18 }) {
  return (
    <button
      onClick={onClick}
      title={label}
      style={{
        display: "flex",
        flexDirection: vertical ? "column" : "row",
        alignItems: "center",
        gap: vertical ? 4 : 6,
        background: active ? "var(--btn-pressed-bg)" : "transparent",
        color: active ? "var(--text-on-accent)" : "var(--text-secondary)",
        border: `1px solid ${active ? "var(--border-accent)" : "transparent"}`,
        borderRadius: "var(--radius-sm)",
        padding: vertical ? "10px 6px" : "6px 10px",
        cursor: "pointer",
        fontFamily: "var(--font-titling)",
        fontSize: vertical ? "11px" : "12px",
        letterSpacing: "var(--tracking-label)",
        textTransform: "uppercase",
        textAlign: "center",
        transition: "background 90ms ease, color 90ms ease, border-color 90ms ease",
        lineHeight: 1.15
      }}>
      
      <Icon name={icon} size={size} />
      <span>{label}</span>
    </button>);

}

function IconOnlyButton({ icon, active, disabled, onClick, title }) {
  const [hover, setHover] = useState(false);
  return (
    <button
      onClick={disabled ? undefined : onClick}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
      title={title}
      disabled={disabled}
      style={{
        width: 38, height: 38, display: "flex", alignItems: "center", justifyContent: "center",
        background: active ? "var(--btn-pressed-bg)" : hover ? "var(--btn-highlighted-bg)" : "var(--btn-normal-bg)",
        color: disabled ? "var(--text-disabled)" : active ? "var(--text-on-accent)" : "var(--text-primary)",
        border: `1px solid ${active ? "var(--border-accent)" : "var(--border-default)"}`,
        borderRadius: "var(--radius-sm)",
        cursor: disabled ? "not-allowed" : "pointer",
        transition: "background 90ms ease"
      }}>
      
      <Icon name={icon} size={19} />
    </button>);

}

function ToolbarStrip({ children, align }) {
  return (
    <div style={{
      display: "flex", alignItems: "center", gap: 6,
      background: "var(--surface-panel)",
      border: "1px solid var(--border-default)",
      borderRadius: "var(--radius-md)",
      boxShadow: "var(--shadow-window)",
      padding: 6,
      justifyContent: align || "flex-start"
    }}>
      {children}
    </div>);

}

function Sep() {
  return <div style={{ width: 1, alignSelf: "stretch", background: "var(--border-default)", margin: "2px 2px" }} />;
}

function Popover({ title, onClose, children, width = 260 }) {
  return (
    <div style={{ position: "absolute", top: "calc(100% + 8px)", right: 0, width, zIndex: 100 }}>
      <Window title={title} onClose={onClose}>
        {children}
      </Window>
    </div>);

}

function MiniToggleRow({ label, checked, onChange }) {
  return (
    <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", padding: "6px 0" }}>
      <span style={{ fontFamily: "var(--font-body)", fontSize: "var(--text-body-sm)", color: "var(--text-secondary)" }}>{label}</span>
      <button
        onClick={() => onChange(!checked)}
        style={{
          width: 36, height: 20, borderRadius: 999, border: "1px solid var(--border-default)",
          background: checked ? "var(--accent-rust)" : "var(--surface-inset)",
          position: "relative", cursor: "pointer"
        }}>
        
        <div style={{
          position: "absolute", top: 1, left: checked ? 17 : 1, width: 16, height: 16, borderRadius: "50%",
          background: "var(--ramp-steel-100)", transition: "left 120ms ease"
        }} />
      </button>
    </div>);

}

function MiniSlider({ label, value, min, max, onChange, unit }) {
  return (
    <div style={{ padding: "6px 0" }}>
      <div style={{ display: "flex", justifyContent: "space-between", fontFamily: "var(--font-body)", fontSize: "var(--text-body-sm)", color: "var(--text-secondary)", marginBottom: 4 }}>
        <span>{label}</span>
        <span style={{ fontFamily: "var(--font-terminal)", color: "var(--text-primary)" }}>{value}{unit}</span>
      </div>
      <input type="range" min={min} max={max} value={value} onChange={(e) => onChange(Number(e.target.value))} style={{ width: "100%", accentColor: "var(--accent-rust)" }} />
    </div>);

}

/* ---------------------------------------------------------------------- */
/* App                                                                     */
/* ---------------------------------------------------------------------- */

function App() {
  const [tool, setTool] = useState("select");
  const [modeKey, setModeKey] = useState("upper");
  const [subcatKey, setSubcatKey] = useState("flooring");
  const [search, setSearch] = useState("");
  const [selectedItem, setSelectedItem] = useState({ label: "Steel Plating", tint: SWATCH_TINTS[0] });
  const [hideUI, setHideUI] = useState(false);
  const [openPopover, setOpenPopover] = useState(null);
  const [toast, setToast] = useState(null);
  const [undoStack, setUndoStack] = useState(3);
  const [redoStack, setRedoStack] = useState(0);
  const [layerVis, setLayerVis] = useState({ upper: true, lower: true, piping: false });
  const [camera, setCamera] = useState({ fov: 72, speed: 5, rotSpeed: 4 });
  const [settings, setSettings] = useState({ snap: true, debug: false });

  useEffect(() => {
    if (!toast) return;
    const t = setTimeout(() => setToast(null), 1600);
    return () => clearTimeout(t);
  }, [toast]);

  const mode = MODES.find((m) => m.key === modeKey);
  const subcat = mode.subcats ? mode.subcats.find((s) => s.key === subcatKey) || mode.subcats[0] : null;
  const itemNames = mode.subcats ? subcat.items : mode.items;
  const filtered = itemNames.filter((n) => n.toLowerCase().includes(search.toLowerCase()));

  function pickMode(key) {
    setModeKey(key);
    const m = MODES.find((x) => x.key === key);
    if (m.subcats) setSubcatKey(m.subcats[0].key);
  }

  function togglePopover(key) {
    setOpenPopover(openPopover === key ? null : key);
  }

  return (
    <div style={{
      position: "absolute", inset: 0, overflow: "hidden",
      background: "radial-gradient(ellipse 70% 60% at 50% 30%, var(--ramp-space-200) 0%, var(--ramp-space-400) 55%, var(--ramp-space-500) 100%)",
      fontFamily: "var(--font-body)"
    }}>
      {/* faint corridor placeholder atmosphere — not a rendered scene */}
      <div style={{ position: "absolute", inset: 0, background: "linear-gradient(180deg, rgba(255,255,255,0.02), transparent 40%)" }} />
      <div style={{ position: "absolute", left: "8%", top: 0, bottom: 0, width: "20%", background: "linear-gradient(90deg, rgba(0,0,0,0.35), transparent)" }} />
      <div style={{ position: "absolute", right: "8%", top: 0, bottom: 0, width: "20%", background: "linear-gradient(270deg, rgba(0,0,0,0.35), transparent)" }} />
      <div style={{
        position: "absolute", left: "50%", top: "38%", width: 2, height: 220, transform: "translateX(-50%)",
        background: "linear-gradient(180deg, rgba(255,255,255,0.06), transparent)"
      }} />
      <div style={{
        position: "absolute", left: 24, top: 24, right: 24, textAlign: "center", color: "var(--text-disabled)",
        fontFamily: "var(--font-terminal)", fontSize: 12, letterSpacing: "0.08em", pointerEvents: "none"
      }}>
        [ VIEWPORT — STATION CORRIDOR PLACEHOLDER ]
      </div>

      {/* HUD layer (hideable) */}
      <div style={{
        position: "absolute", inset: 0,
        opacity: hideUI ? 0 : 1,
        pointerEvents: hideUI ? "none" : "auto",
        transition: "opacity var(--duration-medium) var(--ease-standard)"
      }}>
        {/* Exit */}
        <div style={{ position: "absolute", top: 24, left: 24 }}>
          <Button variant="steel" onClick={() => setToast("Exit requires confirmation (not wired in mockup).")}>
            <span style={{ display: "flex", alignItems: "center", gap: 8 }}><Icon name="exit" size={16} /> Exit</span>
          </Button>
        </div>

        {/* Tools menu */}
        <div style={{ position: "absolute", top: 24, left: "50%", transform: "translateX(-50%)", zIndex: 50 }}>
          <ToolbarStrip>
            <IconOnlyButton icon="select" title="Select" active={tool === "select"} onClick={() => setTool("select")} />
            <IconOnlyButton icon="edit" title="Edit" active={tool === "edit"} onClick={() => setTool("edit")} />
            <IconOnlyButton icon="move" title="Move" active={tool === "move"} onClick={() => setTool("move")} />
            <Sep />
            <IconOnlyButton icon="undo" title="Undo" disabled={undoStack === 0} onClick={() => {setUndoStack((s) => s - 1);setRedoStack((s) => s + 1);}} />
            <IconOnlyButton icon="redo" title="Redo" disabled={redoStack === 0} onClick={() => {setRedoStack((s) => s - 1);setUndoStack((s) => s + 1);}} />
            <Sep />
            <IconOnlyButton icon="quicksave" title="Quicksave" onClick={() => setToast("Quicksaved.")} />
            <IconOnlyButton icon="openMap" title="Open map selection" active={openPopover === "maps"} onClick={() => togglePopover("maps")} />
          </ToolbarStrip>
          {openPopover === "maps" &&
          <Popover title="Map Selection" onClose={() => setOpenPopover(null)} width={280}>
              {["Quicksave — 14:02", "Quicksave — 13:47", "Quicksave — 13:20", "autosave_003"].map((q) =>
            <div key={q} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: "6px 0", borderBottom: "1px solid var(--border-subtle)" }}>
                  <span style={{ fontFamily: "var(--font-terminal)", fontSize: 13, color: "var(--text-primary)" }}>{q}</span>
                  <span style={{ display: "flex", gap: 6 }}>
                    <Badge tone="info">Load</Badge>
                  </span>
                </div>
            )}
            </Popover>
          }
        </div>

        {/* View menu */}
        <div style={{ position: "absolute", top: 24, right: 24 }}>
          <ToolbarStrip>
            <IconOnlyButton icon="resetPosition" title="Reset position" onClick={() => setToast("View reset.")} />
            <IconOnlyButton icon="layers" title="Layer view mode" active={openPopover === "layers"} onClick={() => togglePopover("layers")} />
            <IconOnlyButton icon={hideUI ? "eyeOff" : "eye"} title="Hide UI" active={hideUI} onClick={() => setHideUI(true)} />
            <IconOnlyButton icon="camera" title="Camera options" active={openPopover === "camera"} onClick={() => togglePopover("camera")} />
            <IconOnlyButton icon="settings" title="Map editor settings" active={openPopover === "settings"} onClick={() => togglePopover("settings")} />
          </ToolbarStrip>
          {openPopover === "layers" &&
          <Popover title="Layer View Mode" onClose={() => setOpenPopover(null)}>
              <MiniToggleRow label="Show Upper" checked={layerVis.upper} onChange={(v) => setLayerVis((l) => ({ ...l, upper: v }))} />
              <MiniToggleRow label="Show Lower" checked={layerVis.lower} onChange={(v) => setLayerVis((l) => ({ ...l, lower: v }))} />
              <MiniToggleRow label="Show Piping" checked={layerVis.piping} onChange={(v) => setLayerVis((l) => ({ ...l, piping: v }))} />
            </Popover>
          }
          {openPopover === "camera" &&
          <Popover title="Camera Options" onClose={() => setOpenPopover(null)}>
              <MiniSlider label="Field of View" min={50} max={110} value={camera.fov} unit="°" onChange={(v) => setCamera((c) => ({ ...c, fov: v }))} />
              <MiniSlider label="Camera Speed" min={1} max={10} value={camera.speed} onChange={(v) => setCamera((c) => ({ ...c, speed: v }))} />
              <MiniSlider label="Rotation Speed" min={1} max={10} value={camera.rotSpeed} onChange={(v) => setCamera((c) => ({ ...c, rotSpeed: v }))} />
            </Popover>
          }
          {openPopover === "settings" &&
          <Popover title="Map Editor Settings" onClose={() => setOpenPopover(null)}>
              <MiniToggleRow label="Grid Snap" checked={settings.snap} onChange={(v) => setSettings((s) => ({ ...s, snap: v }))} />
              <MiniToggleRow label="Debug Overlay" checked={settings.debug} onChange={(v) => setSettings((s) => ({ ...s, debug: v }))} />
              <div style={{ fontFamily: "var(--font-body)", fontSize: "var(--text-body-sm)", color: "var(--text-tertiary)", paddingTop: 8 }}>
                Keymap editing not shown in this mockup.
              </div>
            </Popover>
          }
        </div>

        {/* Selected object panel */}
        <div style={{ position: "absolute", top: 108, left: "50%", transform: "translateX(-50%)", zIndex: 10 }}>
          <Window title="Selected Object">
            <div style={{ display: "flex", gap: 14, width: 420 }}>
              <div style={{
                width: 64, height: 64, flexShrink: 0, borderRadius: "var(--radius-md)",
                background: selectedItem.tint, border: "1.5px solid var(--border-strong)",
                display: "flex", alignItems: "center", justifyContent: "center",
                color: "var(--text-on-accent)", fontFamily: "var(--font-titling)", fontSize: 20
              }}>
                {selectedItem.label.charAt(0)}
              </div>
              <div style={{ flex: 1, display: "flex", flexDirection: "column", gap: 8 }}>
                <div style={{ fontFamily: "var(--font-titling)", fontSize: "var(--text-heading-md)", letterSpacing: "var(--tracking-wide)", color: "var(--text-primary)" }}>
                  {selectedItem.label}
                </div>
                <div style={{ fontFamily: "var(--font-terminal)", fontSize: 12, color: "var(--text-tertiary)" }}>
                  Placement hint · Rotation 0° · Snap {settings.snap ? "On" : "Off"}
                </div>
                <div style={{
                  border: `1px solid ${tool === "edit" ? "var(--border-accent)" : "var(--border-default)"}`,
                  background: "var(--surface-inset)", borderRadius: "var(--radius-sm)",
                  padding: "6px 10px", fontFamily: "var(--font-terminal)", fontSize: 12,
                  color: tool === "edit" ? "var(--accent-rust)" : "var(--text-disabled)"
                }}>
                  {tool === "edit" ? "Edit tool active — click a tile to place" : "Select the Edit tool to place this object"}
                </div>
              </div>
            </div>
          </Window>
        </div>

        {/* Bottom dock: mode rail + object library */}
        <div style={{ position: "absolute", left: 24, right: 24, bottom: 24, height: 336, display: "flex", gap: 12 }}>
          <div style={{
            width: 108, background: "var(--surface-panel)", border: "1px solid var(--border-default)",
            borderRadius: "var(--radius-md)", boxShadow: "var(--shadow-window)",
            display: "flex", flexDirection: "column", gap: 4, padding: 8
          }}>
            {MODES.map((m) =>
            <IconTab key={m.key} icon={m.icon} label={m.label} vertical active={modeKey === m.key} onClick={() => pickMode(m.key)} />
            )}
          </div>

          <div style={{ flex: 1, minWidth: 0 }}>
            <Window title={`Object Library — ${mode.label}${subcat ? " / " + subcat.label : ""}`}>
              <div style={{ width: "100%", display: "flex", flexDirection: "column", gap: 10 }}>
                <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 12 }}>
                  <div style={{ display: "flex", gap: 4, flexWrap: "wrap" }}>
                    {mode.subcats && mode.subcats.map((s) =>
                    <IconTab key={s.key} icon={s.icon} label={s.label} active={subcatKey === s.key} onClick={() => setSubcatKey(s.key)} size={15} />
                    )}
                  </div>
                  <div style={{
                    display: "flex", alignItems: "center", gap: 8, background: "var(--surface-inset)",
                    border: "1px solid var(--border-default)", borderRadius: "var(--radius-sm)",
                    padding: "6px 10px", minWidth: 220, boxShadow: "var(--shadow-inset)"
                  }}>
                    <Icon name="search" size={16} color="var(--text-tertiary)" />
                    <input
                      value={search}
                      onChange={(e) => setSearch(e.target.value)}
                      placeholder="Search tags, names, keywords…"
                      style={{
                        background: "transparent", border: "none", outline: "none", flex: 1,
                        color: "var(--text-primary)", fontFamily: "var(--font-body)", fontSize: "var(--text-body-sm)"
                      }} />
                    
                  </div>
                </div>

                <div style={{
                  display: "grid", gridTemplateColumns: "repeat(9, 1fr)", gap: 8,
                  maxHeight: 190, overflowY: "auto", paddingRight: 4
                }}>
                  {filtered.map((name, i) => {
                    const tint = SWATCH_TINTS[i % SWATCH_TINTS.length];
                    const active = selectedItem.label === name;
                    return (
                      <button
                        key={name}
                        onClick={() => setSelectedItem({ label: name, tint })}
                        title={name}
                        style={{
                          display: "flex", flexDirection: "column", alignItems: "center", gap: 4,
                          background: "var(--surface-panel-raised)",
                          border: `1.5px solid ${active ? "var(--border-accent)" : "var(--border-default)"}`,
                          borderRadius: "var(--radius-md)", padding: "8px 4px 6px", cursor: "pointer"
                        }}>
                        
                        <div style={{ width: 34, height: 34, borderRadius: "var(--radius-sm)", background: tint }} />
                        <span style={{
                          fontFamily: "var(--font-body)", fontSize: 10.5, color: "var(--text-secondary)",
                          textAlign: "center", lineHeight: 1.2, overflow: "hidden", textOverflow: "ellipsis",
                          whiteSpace: "nowrap", width: "100%"
                        }}>
                          {name}
                        </span>
                      </button>);

                  })}
                  {filtered.length === 0 &&
                  <div style={{ gridColumn: "1 / -1", color: "var(--text-disabled)", fontFamily: "var(--font-body)", fontSize: 13, padding: "20px 0", textAlign: "center" }}>
                      No matches for “{search}”.
                    </div>
                  }
                </div>
              </div>
            </Window>
          </div>
        </div>
      </div>

      {/* Hide-UI reveal tab — always present, outside the hideable layer */}
      {hideUI &&
      <button
        onClick={() => setHideUI(false)}
        title="Show UI"
        style={{
          position: "absolute", top: 16, right: 16, width: 34, height: 34, borderRadius: "var(--radius-sm)",
          background: "rgba(23,26,30,0.7)", border: "1px solid var(--border-default)",
          color: "var(--text-secondary)", display: "flex", alignItems: "center", justifyContent: "center",
          cursor: "pointer", zIndex: 50
        }}>
        
          <Icon name="eyeOff" size={18} />
        </button>
      }

      {/* Toast */}
      {toast &&
      <div style={{
        position: "absolute", bottom: 40, left: "50%", transform: "translateX(-50%)",
        background: "var(--surface-panel-raised)", border: "1px solid var(--border-accent)",
        borderRadius: "var(--radius-sm)", padding: "10px 18px", color: "var(--text-primary)",
        fontFamily: "var(--font-terminal)", fontSize: 13, boxShadow: "var(--shadow-panel)", zIndex: 60
      }}>
          {toast}
        </div>
      }
    </div>);

}

ReactDOM.createRoot(document.getElementById("root")).render(<App />);