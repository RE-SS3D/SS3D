# Diagnostic & hacking interface — design document

> Status: active

## 1. Design philosophy

Four rules everything below follows from:

1. **It's a diagnostic tool, not a hack terminal.** The device doesn't know whether the person holding it is a crew member doing routine maintenance or an antag planting a backdoor. It just reads and, if authorized, writes. Whether that's "hacking" is a property of the *player's intent and knowledge*, not a mode the UI switches into.
2. **Same data, different literacy.** There's no difficulty slider. Every player connecting to a given device sees data pulled from the same underlying object. What differs is how much of it their firmware/access/knowledge lets them *resolve into meaning*. A crew member's tool shows friendly labels because it has vendor drivers. A hacker's tool shows raw registers because it doesn't — and reading raw registers is a skill, not a stat.
3. **Everything on screen maps to something physical.** No abstract graphs, no "firewall HP." A network panel is a wiring diagram that matches the actual station wiring. A hardware panel is the actual board layout of the actual object. If you can't point at the real component the screen element represents, it doesn't belong on screen.
4. **Discovery and failure are physical, not RNG.** You find devices because you're in range, in line of sight, or physically jacked in — not because you clicked "scan network." You fail a hack because you guessed the wrong protocol, missed a step, or tripped a sensor — not because a percentage roll went against you.

## 2. The device

A handheld unit — call it the **field diagnostic unit (FDU)** pending whatever name fits existing SS3D item lore (it can reuse/extend the multitool). Modeled and animated as a real held object, not a UI overlay that appears from nowhere.

**Two ways to connect:**

- **Wireless scan** — passively picks up RF broadcast from powered devices within range. Range and reliability degrade with walls and distance (reuses the tilemap for line-of-sight/occlusion). Anything not broadcasting (older or deliberately shielded hardware) is invisible this way.
- **Wired jack** — after removing a maintenance panel with a screwdriver, plug a data cable into an exposed port. Required for shielded/hardwired-only systems, and generally gives higher bandwidth and can reach things a wireless scan can't touch. This is the physical-access tradeoff that makes hacking feel like reverse engineering a real object instead of clicking a menu.

When connected, the device's own small screen becomes the interface — rendered as a diegetic HUD element (like reading a screen in your hands), not a full-screen takeover.

## 3. The seven panels

### Device discovery
Not an omniscient scanner list. What shows up is filtered by range, shielding, and connection method. Entries start as raw identifiers (hardware ID, signal strength, frequency) — a friendly name only appears if the device's manifest is one your firmware already trusts (all standard station equipment, for crew and engineers) or one you've fingerprinted yourself. Fingerprinting an unknown device is an active step: ping it, read the response, compare against known protocol signatures. It takes time and can return a wrong guess.

### Hardware overview
A schematic of the target's actual internal board: control MCU, sensor array, actuator driver, power regulation, backup battery, tamper switch, comms module, firmware ROM — literally the parts list of the object's model. Not present for crew (no clearance to see it at all). Full board + per-part health for engineers. Register addresses, firmware hash, and any undocumented test pads for hackers.

### Network connections
The device's real place in the station's actual networks — power (which APC/breaker it draws from), data (which hub/segment), pipe net if relevant — shown as a wiring trace that matches the physical layout, not an abstract graph. Crew see only "powered / networked." Engineers see the full trace back to source. Hackers additionally see which segments are encrypted vs. legacy/plaintext — a real weak point to pivot from, not a hack-bar.

### System permissions
Built on the game's existing ID/access system. Shows which access levels operate the device, the current session, and (for engineers) an audit log of recent authentications. Crew see only whether their own ID would work. Hackers see the raw access bitmask and where a cloned or forged credential would need to match it — enabling credential spoofing as a technique rather than a "bypass %" button.

### Component status
Live values read from the device's actual simulated state (temperature, wear, power draw, error codes) — the same kind of live data your atmospherics/tilemap systems already track for other objects. Crew get an OK/fault light. Engineers get numeric readouts, trends, and human-readable fault codes. Hackers get raw register values, including any that have been frozen or spoofed by prior tampering — useful both for detecting someone else's hack and for planting your own.

### Security layers
Represented as the real things they'd be on real equipment: a physical tamper switch, ID/credential authentication, firmware signing against a known-good release, and encryption on the data bus. Not a meter. Crew see none of it. Engineers see which layers are present and healthy — informational, for legitimate repair. Hackers see which layers are actually bypassable and how: disable the tamper switch physically (leaves evidence for a future audit), replay or spoof an intercepted credential, load unsigned firmware through a slot the vendor left open, or use a cert compromised elsewhere. Detection risk is tied to real logs and alarms, not an abstract countdown.

### Available modifications
Context-sensitive to what's actually been achieved. Legitimate, engineer-tier actions: recalibrate a sensor, replace a failing part, install an official firmware update, adjust a threshold setpoint. Antag-tier actions only unlock once the relevant security layer is actually bypassed: flash unsigned firmware, force an actuator regardless of auth, spoof a sensor feed, blind an alarm, plant a persistent backdoor cert. Each one states its physical/systemic consequence (heat, a written log entry, a tripped breaker) instead of a success percentage.

## 4. Three tiers, one device

The tiers aren't a UI setting the player picks — they fall out of what firmware and access the player's character and tool actually have:

- **Crew** run consumer firmware locked to the public interface. They see a resolved, minimal, "smart lock app" view: name, status, whether their own ID works. Nothing else exists to them.
- **Engineers** run maintenance-mode firmware unlocked by an engineering credential. This is the "OBD-II scanner" view — full technical detail, but still entirely legitimate; nothing here required bypassing anything.
- **Hackers** are running something the manufacturer didn't sign, or have found a real weakness to exploit. Nothing is resolved for them automatically — names, permissions, and modification options only become available as they're actually reverse-engineered or unlocked in play.

## 5. Worked example — AIRLOCK-ENG-03

| | Crew | Engineer | Hacker |
|---|---|---|---|
| Discovery | Shows as "Airlock — Engineering Bay 3" | Same, plus hardware ID and firmware version | Shows as raw hardware ID until fingerprinted |
| Hardware | Not visible | Full board diagram, all parts nominal | Board diagram plus an undocumented test pad the vendor left in |
| Network | Not visible | Full trace to APC/breaker and data hub | Same trace, plus a flagged plaintext legacy segment |
| Permissions | "Your ID: authorized" | Full ACL table and last-auth log | Raw bitmask; engineering bit matches a clonable cert |
| Component status | Not visible | Motor temp, cycle count, no faults | Same as registers, one flagged as not updated in days |
| Security | Not visible | Tamper sealed, firmware signed, auth active | Tamper bypassed, firmware unverified, replay window open |
| Modifications | None | Recalibrate, replace part, update firmware, adjust threshold | Flash unsigned firmware, force override, spoof feed, plant backdoor (locked pending a comms key) |

## 6. Hooks into existing SS3D systems

- **Permissions panel** reads directly off the existing ID/access-level system — no parallel permission model to maintain.
- **Network panel** reflects the actual power net and data net topology already simulated for other systems, so the panel is a view onto real state rather than a fake diagram authored per-device.
- **Discovery range/occlusion** reuses the tilemap for line-of-sight, the same way it would matter for any other physics-adjacent system.
- **Component status** reads the same kind of live per-object simulated values the atmospherics work already tracks elsewhere, applied to electrical/mechanical devices instead of gas.
- **Security bypass consequences** should write to whatever station logging/camera/alarm systems exist (or are planned), so detection risk is a real, inspectable trail rather than a hidden number.

## 7. What this avoids, and how

- **Wire puzzle gameplay** — there are no wires to trace-and-connect in the UI; the network panel *reports* real wiring, it doesn't ask you to solve it as a puzzle.
- **Generic cyberpunk hacking screens** — no neon, no matrix-style scroll, no abstract "breach" animation. The device looks and reads like an industrial diagnostic tool (think OBD-II scanner or a multimeter), because that's what it is.
- **Abstract node games** — every panel is either a real schematic, a real table, or a real log. Nothing is a graph of generic nodes with no referent.
- **Random minigames** — bypassing security is a sequence of concrete, checkable actions (disable a switch, replay a credential, load an image) with real preconditions and consequences, not a skill-check minigame layered on top.
