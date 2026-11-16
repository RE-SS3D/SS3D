// Flat, single-color line icons matching the SS3D/Heroicons-outline weight
// (24x24 grid, ~1.6 stroke, round caps/joins). Custom-drawn to cover tools
// that have no vendored sprite equivalent.

const ICONS = {
  select: (
    <path fill="currentColor" stroke="none"
      d="M5 3.5v16.2l3.6-3.1 2.2 5 2.3-1-2.2-5h4.4L5 3.5z" />
  ),
  edit: (
    <>
      <path d="M15.6 4.6l3.8 3.8-9.9 9.9-4.7 1 1-4.7 9.8-10z" />
      <path d="M13.6 6.6l3.8 3.8" />
    </>
  ),
  move: (
    <>
      <path d="M12 3v18M3 12h18" />
      <path d="M12 3l-2.3 2.3M12 3l2.3 2.3M12 21l-2.3-2.3M12 21l2.3-2.3" />
      <path d="M3 12l2.3-2.3M3 12l2.3 2.3M21 12l-2.3-2.3M21 12l-2.3 2.3" />
    </>
  ),
  undo: (
    <>
      <path d="M9 8L4.5 12 9 16" />
      <path d="M4.5 12H14a5.5 5.5 0 010 11h-2" />
    </>
  ),
  redo: (
    <>
      <path d="M15 8l4.5 4-4.5 4" />
      <path d="M19.5 12H10a5.5 5.5 0 000 11h2" />
    </>
  ),
  quicksave: (
    <>
      <path d="M4.5 4h12l3.5 3.5V19a1 1 0 01-1 1H4.5a1 1 0 01-1-1V5a1 1 0 011-1z" />
      <path d="M8 4v5.5h7V4" />
      <path d="M7.5 14.5h9V20h-9z" />
    </>
  ),
  openMap: (
    <>
      <path d="M3.5 6.5a1 1 0 011-1H10l1.7 2h7.8a1 1 0 011 1v9.5a1 1 0 01-1 1H4.5a1 1 0 01-1-1V6.5z" />
      <path d="M3.7 11.5h16.6" />
    </>
  ),
  resetPosition: (
    <>
      <circle cx="12" cy="12" r="7" />
      <path d="M12 3v3.2M12 17.8V21M3 12h3.2M17.8 12H21" />
      <circle cx="12" cy="12" r="1.3" fill="currentColor" stroke="none" />
    </>
  ),
  layers: (
    <>
      <path d="M12 3.5l8 4.3-8 4.3-8-4.3 8-4.3z" />
      <path d="M4 12.2l8 4.3 8-4.3" />
      <path d="M4 16.2l8 4.3 8-4.3" />
    </>
  ),
  eye: (
    <>
      <path d="M2 12s3.6-6.5 10-6.5S22 12 22 12s-3.6 6.5-10 6.5S2 12 2 12z" />
      <circle cx="12" cy="12" r="2.8" />
    </>
  ),
  eyeOff: (
    <>
      <path d="M4 4l16 16" />
      <path d="M9.9 5.7A10.6 10.6 0 0112 5.5c6.4 0 10 6.5 10 6.5a15.8 15.8 0 01-3.2 4.1M6.6 6.9A15.4 15.4 0 002 12s3.6 6.5 10 6.5a10.6 10.6 0 004.3-.9" />
      <path d="M9.9 10a3 3 0 004.1 4.1" />
    </>
  ),
  camera: (
    <>
      <path d="M9 5l1.2-2h3.6L15 5h4a1 1 0 011 1v11a1 1 0 01-1 1H5a1 1 0 01-1-1V6a1 1 0 011-1h4z" />
      <circle cx="12" cy="12.5" r="3.6" />
    </>
  ),
  settings: (
    <>
      <circle cx="12" cy="12" r="3" />
      <path d="M12 3v2.2M12 18.8V21M3 12h2.2M18.8 12H21M5.6 5.6l1.6 1.6M16.8 16.8l1.6 1.6M18.4 5.6l-1.6 1.6M7.2 16.8l-1.6 1.6" />
    </>
  ),
  exit: (
    <>
      <path d="M13 4H6a1 1 0 00-1 1v14a1 1 0 001 1h7" />
      <path d="M11 12h9M20 12l-3.2-3.2M20 12l-3.2 3.2" />
    </>
  ),
  search: (
    <>
      <circle cx="10.5" cy="10.5" r="6.5" />
      <path d="M19.5 19.5l-4.4-4.4" />
    </>
  ),
  chevronUp: <path d="M5 15l7-7 7 7" />,
  chevronDown: <path d="M5 9l7 7 7-7" />,
  box: (
    <>
      <path d="M12 3.5l8 4.3v8.4l-8 4.3-8-4.3V7.8l8-4.3z" />
      <path d="M4 7.8L12 12l8-4.2M12 12v9.5" />
    </>
  ),
  script: (
    <>
      <path d="M8 4L3.5 12 8 20" />
      <path d="M16 4l4.5 8-4.5 8" />
    </>
  ),
  grid: (
    <>
      <rect x="4" y="4" width="7" height="7" />
      <rect x="13" y="4" width="7" height="7" />
      <rect x="4" y="13" width="7" height="7" />
      <rect x="13" y="13" width="7" height="7" />
    </>
  ),
  terrain: (
    <>
      <path d="M3 9c2-2 3.5-2 5.5 0s3.5 2 5.5 0 3.5-2 5.5 0" />
      <path d="M3 15c2-2 3.5-2 5.5 0s3.5 2 5.5 0 3.5-2 5.5 0" />
    </>
  ),
  door: (
    <>
      <rect x="5.5" y="3.5" width="13" height="17" rx="0.5" />
      <circle cx="14.7" cy="12.3" r="0.9" fill="currentColor" stroke="none" />
    </>
  ),
  cube: (
    <>
      <path d="M12 3.5l7 4v9l-7 4-7-4v-9l7-4z" />
      <path d="M5 7.5L12 11.5 19 7.5M12 11.5V20" />
    </>
  ),
  bracket: (
    <>
      <rect x="5" y="9" width="14" height="10" />
      <path d="M8 9V6a1 1 0 011-1h6a1 1 0 011 1v3" />
    </>
  ),
  pipe: (
    <>
      <path d="M4 6h7a4 4 0 014 4v8" />
      <path d="M4 6v0" />
      <circle cx="4" cy="6" r="1.6" fill="currentColor" stroke="none" />
      <circle cx="15" cy="18" r="1.6" fill="currentColor" stroke="none" />
    </>
  ),
  trash: (
    <>
      <path d="M5 7h14M9 7V5a1 1 0 011-1h4a1 1 0 011 1v2" />
      <path d="M6.5 7l1 12.5a1 1 0 001 1h7l1-12.5" />
    </>
  ),
  dots9: (
    <>
      {[5, 12, 19].map((y) =>
        [5, 12, 19].map((x) => (
          <circle key={x + "-" + y} cx={x} cy={y} r="1.5" fill="currentColor" stroke="none" />
        ))
      )}
    </>
  ),
  pin: (
    <>
      <path d="M12 21s7-6.5 7-11.5a7 7 0 10-14 0C5 14.5 12 21 12 21z" />
      <circle cx="12" cy="9.5" r="2.4" />
    </>
  ),
  dice: (
    <>
      <rect x="4" y="4" width="16" height="16" rx="1.5" />
      <circle cx="8.3" cy="8.3" r="1.1" fill="currentColor" stroke="none" />
      <circle cx="15.7" cy="8.3" r="1.1" fill="currentColor" stroke="none" />
      <circle cx="12" cy="12" r="1.1" fill="currentColor" stroke="none" />
      <circle cx="8.3" cy="15.7" r="1.1" fill="currentColor" stroke="none" />
      <circle cx="15.7" cy="15.7" r="1.1" fill="currentColor" stroke="none" />
    </>
  ),
  bolt: (
    <path fill="currentColor" stroke="none" d="M13 2L4 14h6l-1 8 9-12h-6l1-8z" />
  ),
};

function Icon({ name, size = 20, color, style }) {
  const content = ICONS[name];
  if (!content) return null;
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke={color || "currentColor"}
      strokeWidth="1.6"
      strokeLinecap="round"
      strokeLinejoin="round"
      style={style}
    >
      {content}
    </svg>
  );
}

window.Icon = Icon;
