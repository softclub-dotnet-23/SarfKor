import { LogoMark } from '../Logo'

interface Tile {
  label: string
  bg: string
  glyph: React.ReactNode
  highlight?: boolean
}

const g = (path: string, opts: { fill?: string; viewBox?: string } = {}) => (
  <svg viewBox={opts.viewBox ?? '0 0 24 24'} width="52%" height="52%" fill={opts.fill ?? '#fff'}>
    <path d={path} />
  </svg>
)

const apps: Tile[] = [
  { label: 'FaceTime', bg: 'linear-gradient(180deg,#8CE05A,#3FA637)', glyph: g('M17 8.5 21 6v12l-4-2.5v-7Zm-14-2A1.5 1.5 0 0 1 4.5 5h9A1.5 1.5 0 0 1 15 6.5v11a1.5 1.5 0 0 1-1.5 1.5h-9A1.5 1.5 0 0 1 3 17.5v-11Z') },
  { label: 'Photos', bg: 'conic-gradient(from 200deg,#FFB400,#FF5A5F,#C74BFF,#3E8CFF,#2FD16A,#FFB400)', glyph: g('M12 6.5a5.5 5.5 0 1 1 0 11 5.5 5.5 0 0 1 0-11Z', { fill: '#fff' }) },
  { label: 'Camera', bg: 'linear-gradient(180deg,#4a4a4a,#1c1c1e)', glyph: g('M9 4h6l1.2 2H19a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h2.8L9 4Zm3 5.4a3.6 3.6 0 1 0 0 7.2 3.6 3.6 0 0 0 0-7.2Z') },
  { label: 'Calendar', bg: '#ffffff', glyph: <div className="flex h-full w-full flex-col overflow-hidden rounded-[22%]"><div className="w-full bg-[#FF3B30] text-center font-semibold text-white leading-tight" style={{ fontSize: '3.6cqw' }}>ПТ</div><div className="flex flex-1 items-center justify-center font-bold text-neutral-800" style={{ fontSize: '7.5cqw' }}>18</div></div> },
  { label: 'Mail', bg: 'linear-gradient(180deg,#4FA8FF,#1E7BFF)', glyph: g('M3 6.5 12 13l9-6.5V7L12 15 3 8.5V6.5Zm0 3.2V17a1 1 0 0 0 1 1h16a1 1 0 0 0 1-1V9.7l-9 6.4-9-6.4Z') },
  { label: 'Clock', bg: 'linear-gradient(180deg,#2c2c2e,#000)', glyph: g('M12 4a8 8 0 1 0 0 16 8 8 0 0 0 0-16Zm.7 3.5v4.3l3.2 1.9-.7 1.2-3.9-2.3V7.5h1.4Z') },
  { label: 'Maps', bg: 'linear-gradient(160deg,#8fd9c4 0%,#eee 40%,#8fc1e8 100%)', glyph: g('M12 3c-3 0-6 2.2-6 6.4C6 14 12 21 12 21s6-7 6-11.6C18 5.2 15 3 12 3Zm0 8a2.4 2.4 0 1 1 0-4.8A2.4 2.4 0 0 1 12 11Z', { fill: '#FF3B30' }) },
  { label: 'Weather', bg: 'linear-gradient(180deg,#4FC3FF,#0A84FF)', glyph: g('M6.5 18a3.8 3.8 0 0 1-.6-7.55 5 5 0 0 1 9.7-1.6A4 4 0 0 1 17 16.9', { fill: 'none' }) },
  { label: 'Reminders', bg: '#ffffff', glyph: g('M9 16.2 5.3 12.5l1.4-1.4L9 13.4l7.3-7.3 1.4 1.4L9 16.2Z', { fill: '#FF9500' }) },
  { label: 'Notes', bg: 'linear-gradient(180deg,#FFD84D,#FFC400)', glyph: g('M5 4h14a1 1 0 0 1 1 1v3H4V5a1 1 0 0 1 1-1Zm-1 6h16v9a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1v-9Z', { fill: '#fff' }) },
  { label: 'Stocks', bg: 'linear-gradient(180deg,#2c2c2e,#000)', glyph: g('M4 19V9h3v10H4Zm6.5 0V4h3v15h-3ZM17 19v-7h3v7h-3Z') },
  { label: 'Books', bg: 'linear-gradient(180deg,#FF9F4D,#FF7A1A)', glyph: g('M5 4.5h6a2 2 0 0 1 2 2V20a2 2 0 0 0-2-1.5H5V4.5Zm14 0v14H13a2 2 0 0 0-2 1.5V6.5a2 2 0 0 1 2-2h6Z') },
  { label: 'App Store', bg: 'linear-gradient(160deg,#3EC6FF,#0A84FF)', glyph: g('M12 4 4 20h16L12 4Zm0 5.5 3.2 6.3H8.8L12 9.5Z') },
  { label: 'Podcasts', bg: 'conic-gradient(from 180deg,#C24CFF,#8A2BE2,#C24CFF)', glyph: g('M12 4a4.5 4.5 0 0 0-4.5 4.5c0 1.9 1.1 3.5 2.7 4.2L9 20h6l-1.2-7.3a4.5 4.5 0 0 0 2.7-4.2A4.5 4.5 0 0 0 12 4Z', { fill: '#fff' }) },
  { label: 'TV', bg: 'linear-gradient(180deg,#2c2c2e,#000)', glyph: g('M4 5h16a1 1 0 0 1 1 1v9a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1V6a1 1 0 0 1 1-1Zm4 14h8v1H8v-1Z') },
  { label: 'Health', bg: '#ffffff', glyph: g('M12 20.5S4 15 4 9.6C4 6.5 6.3 4.5 9 4.5c1.6 0 3 .8 3 .8s1.4-.8 3-.8c2.7 0 5 2 5 5.1 0 5.4-8 10.9-8 10.9Z', { fill: '#FF3B30' }) },
  { label: 'Home', bg: 'linear-gradient(180deg,#7AD97A,#2FAE4E)', glyph: g('M12 3.8 4 10.4V20h5.2v-5.6h5.6V20H20v-9.6L12 3.8Z') },
  { label: 'Wallet', bg: 'linear-gradient(180deg,#2c2c2e,#000)', glyph: g('M4 7a2 2 0 0 1 2-2h11a1 1 0 0 1 0 2H6v10h13a1 1 0 0 0 1-1v-6a1 1 0 0 0-1-1h-3a2 2 0 1 0 0 4', { fill: 'none' }) },
  { label: 'Settings', bg: 'linear-gradient(180deg,#9aa0a8,#6b7076)', glyph: g('M12 8.5a3.5 3.5 0 1 0 0 7 3.5 3.5 0 0 0 0-7Zm8.4 2.2-1.6-.4a6.9 6.9 0 0 0-.6-1.4l.9-1.4a1 1 0 0 0-.1-1.2l-1.3-1.3a1 1 0 0 0-1.2-.1l-1.4.9a6.9 6.9 0 0 0-1.4-.6l-.4-1.6A1 1 0 0 0 12.3 3h-1.8a1 1 0 0 0-1 .8l-.4 1.6c-.5.2-1 .4-1.4.6l-1.4-.9a1 1 0 0 0-1.2.1L3.8 6.5a1 1 0 0 0-.1 1.2l.9 1.4c-.3.4-.5.9-.6 1.4l-1.6.4a1 1 0 0 0-.8 1v1.8a1 1 0 0 0 .8 1l1.6.4c.2.5.4 1 .6 1.4l-.9 1.4a1 1 0 0 0 .1 1.2l1.3 1.3a1 1 0 0 0 1.2.1l1.4-.9c.4.3.9.5 1.4.6l.4 1.6a1 1 0 0 0 1 .8h1.8a1 1 0 0 0 1-.8l.4-1.6c.5-.2 1-.4 1.4-.6l1.4.9a1 1 0 0 0 1.2-.1l1.3-1.3a1 1 0 0 0 .1-1.2l-.9-1.4c.3-.4.5-.9.6-1.4l1.6-.4a1 1 0 0 0 .8-1v-1.8a1 1 0 0 0-.8-1Z') },
  { label: 'Sarfkor', bg: 'transparent', glyph: <LogoMark size={44} />, highlight: true },
]

const dock = [
  { label: 'Phone', bg: 'linear-gradient(180deg,#8CE05A,#3FA637)', glyph: g('M6.6 3.5c.6 0 1.1.4 1.3 1l1 2.8c.2.5.1 1.1-.3 1.5l-1.4 1.4a12 12 0 0 0 5.6 5.6l1.4-1.4c.4-.4 1-.5 1.5-.3l2.8 1c.6.2 1 .7 1 1.3v2.5c0 1-.9 1.7-1.8 1.5C9.9 19.4 4.6 14.1 3.1 5.3 2.9 4.4 3.6 3.5 4.6 3.5h2Z') },
  { label: 'Safari', bg: 'conic-gradient(from 90deg,#3EC6FF,#0A84FF,#3EC6FF)', glyph: g('M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18Zm3.5 5.5-2 5.5-5.5 2 2-5.5 5.5-2Z', { fill: '#fff' }) },
  { label: 'Messages', bg: 'linear-gradient(180deg,#8CE05A,#3FA637)', glyph: g('M12 4C6.9 4 3 7.4 3 11.6c0 2.4 1.3 4.5 3.4 5.9-.1.9-.5 2-1.2 2.9 1.4-.2 2.7-.7 3.7-1.4.9.3 2 .5 3.1.5 5.1 0 9-3.4 9-7.6S17.1 4 12 4Z') },
  { label: 'Music', bg: 'linear-gradient(180deg,#FF6B9A,#FF3B6B)', glyph: g('M9 18a2.5 2.5 0 1 1 0-5 2.5 2.5 0 0 1 0 5Zm10-2.5a2.5 2.5 0 1 1-5 0 2.5 2.5 0 0 1 5 0ZM11 13V5.8L19 4v7.5', { fill: 'none' }) },
]

function AppTile({ tile, hideLabel }: { tile: Tile; hideLabel?: boolean }) {
  return (
    <div className="flex flex-col items-center gap-[3%]">
      <div
        className={`flex aspect-square w-full items-center justify-center rounded-[22%] ${
          tile.highlight ? 'ring-2 ring-white/70' : ''
        }`}
        style={{ background: tile.bg, boxShadow: '0 1px 1px rgba(0,0,0,0.25)' }}
      >
        {tile.glyph}
      </div>
      {!hideLabel && (
        <span
          className="w-full truncate text-center font-medium text-white drop-shadow-sm"
          style={{ fontSize: '2.6cqw' }}
        >
          {tile.label}
        </span>
      )}
    </div>
  )
}

export function HomeScreen() {
  return (
    <div className="relative flex h-full w-full flex-col justify-between bg-gradient-to-b from-[#1b2a52] via-[#111a33] to-[#05070f] px-[6%] pb-[4%] pt-[16%]">
      <div className="grid grid-cols-4 gap-x-[4%] gap-y-[5%]">
        {apps.map((tile) => (
          <AppTile key={tile.label} tile={tile} />
        ))}
      </div>

      <div className="mt-[4%] rounded-[9%] bg-white/15 px-[4%] py-[3%] backdrop-blur-md">
        <div className="grid grid-cols-4 gap-x-[4%]">
          {dock.map((tile) => (
            <AppTile key={tile.label} tile={tile} hideLabel />
          ))}
        </div>
      </div>
    </div>
  )
}
