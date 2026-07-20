import { useEffect, useRef } from 'react'
import clsx from 'clsx'
import { HomeScreen } from './HomeScreen'
import { AppScreen } from './AppScreen'
import { SignalGlyph, WifiGlyph, BatteryGlyph } from '../PhoneFrame'

const WIDTH = 300
const HEIGHT = 620
// Front/back radius and edge thickness are sized together: an edge piece's
// own rounding tops out at half its thickness, so a too-thin edge can never
// match a large front-face corner radius without a visible facet where the
// two curves meet. 44px of thickness (22px max cap radius) against a 30px
// front radius keeps that mismatch down to a couple of px — enough to read
// as one continuous rounded edge instead of two different arcs.
const FACE_Z = 22
const EDGE_DEPTH = 44
const HALF_W = WIDTH / 2
const HALF_H = HEIGHT / 2

function PhoneStatusBar({ light }: { light: boolean }) {
  return (
    <div
      className={clsx(
        'pointer-events-none absolute inset-x-0 top-0 z-10 flex items-center justify-between px-[9%] pt-[4%] font-semibold',
        light ? 'text-white' : 'text-[#0b0f19]',
      )}
      style={{ fontSize: '3.4cqw' }}
    >
      <span>9:41</span>
      <div className="flex items-center gap-[3%]">
        <SignalGlyph />
        <WifiGlyph />
        <BatteryGlyph />
      </div>
    </div>
  )
}

interface Hero3DPhoneProps {
  onInteract?: () => void
}

export function Hero3DPhone({ onInteract }: Hero3DPhoneProps) {
  const stageRef = useRef<HTMLDivElement>(null)
  const wrapRef = useRef<HTMLDivElement>(null)
  const phoneRef = useRef<HTMLDivElement>(null)
  const frontRef = useRef<HTMLDivElement>(null)
  const backRef = useRef<HTMLDivElement>(null)
  const screenRef = useRef<HTMLDivElement>(null)
  const pagesRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const stage = stageRef.current!
    const wrap = wrapRef.current!
    const phone = phoneRef.current!
    const front = frontRef.current!
    const back = backRef.current!
    const screen = screenRef.current!

    let rotY = -22
    let rotX = 6
    let velY = 0
    let velX = 0
    let dragging = false
    let lastX = 0
    let lastY = 0
    let interacted = false
    let mx = 0
    let my = 0
    let raf = 0

    const startDrag = (e: PointerEvent) => {
      if (screen.contains(e.target as Node)) return
      dragging = true
      interacted = true
      onInteract?.()
      lastX = e.clientX
      lastY = e.clientY
      velY = 0
      velX = 0
      stage.setPointerCapture?.(e.pointerId)
    }
    const moveDrag = (e: PointerEvent) => {
      if (!dragging) return
      const dx = e.clientX - lastX
      const dy = e.clientY - lastY
      rotY += dx * 0.45
      rotX -= dy * 0.28
      rotX = Math.max(-32, Math.min(32, rotX))
      velY = dx * 0.45
      velX = -dy * 0.28
      lastX = e.clientX
      lastY = e.clientY
    }
    const endDrag = () => {
      dragging = false
    }
    const trackMouse = (e: PointerEvent) => {
      const b = stage.getBoundingClientRect()
      mx = (e.clientX - b.left) / b.width - 0.5
      my = (e.clientY - b.top) / b.height - 0.5
    }

    stage.addEventListener('pointerdown', startDrag)
    window.addEventListener('pointermove', moveDrag)
    window.addEventListener('pointerup', endDrag)
    stage.addEventListener('pointermove', trackMouse)

    const loop = () => {
      if (!dragging) {
        rotY += velY
        rotX += velX
        velY *= 0.94
        velX *= 0.94
        rotX = Math.max(-32, Math.min(32, rotX))
        if (Math.abs(velY) < 0.05 && Math.abs(velX) < 0.05) {
          const t = performance.now() / 1000
          const targetY = interacted ? -22 + Math.sin(t * 0.5) * 16 + mx * 10 : -22 + Math.sin(t * 0.5) * 20
          const targetX = 6 + Math.cos(t * 0.4) * 5 - my * 8
          rotY += (targetY - rotY) * 0.02
          rotX += (targetX - rotX) * 0.03
        }
      }
      phone.style.transform = `rotateX(${rotX.toFixed(2)}deg) rotateY(${rotY.toFixed(2)}deg)`
      wrap.style.transform = `translateY(${(Math.sin((performance.now() / 1000) * 0.8) * 8).toFixed(1)}px)`
      const facing = Math.cos((rotY * Math.PI) / 180)
      front.style.visibility = facing >= 0 ? 'visible' : 'hidden'
      back.style.visibility = facing >= 0 ? 'hidden' : 'visible'
      raf = requestAnimationFrame(loop)
    }

    // Once scrolled past, the hero was still spending a frame of work every
    // 16ms forever — background cost for a phone nobody's looking at, and
    // one more thing competing for the main thread whenever something else
    // (like the theme toggle) needs it. Only animate while it's on screen.
    let running = false
    const io = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting && !running) {
          running = true
          loop()
        } else if (!entry.isIntersecting && running) {
          running = false
          cancelAnimationFrame(raf)
        }
      },
      { rootMargin: '50% 0px' },
    )
    io.observe(stage)

    return () => {
      io.disconnect()
      cancelAnimationFrame(raf)
      stage.removeEventListener('pointerdown', startDrag)
      window.removeEventListener('pointermove', moveDrag)
      window.removeEventListener('pointerup', endDrag)
      stage.removeEventListener('pointermove', trackMouse)
    }
  }, [onInteract])

  useEffect(() => {
    const screen = screenRef.current!
    const pages = pagesRef.current!
    let page = 0
    let dragging = false
    let startX = 0
    let curX = 0

    const setX = (px: number, anim: boolean) => {
      pages.style.transition = anim ? 'transform .5s cubic-bezier(.16,1,.3,1)' : 'none'
      pages.style.transform = `translateX(calc(${-page * 50}% + ${px}px))`
    }
    const down = (e: PointerEvent) => {
      dragging = true
      startX = e.clientX
      curX = e.clientX
      e.stopPropagation()
      screen.setPointerCapture?.(e.pointerId)
    }
    const move = (e: PointerEvent) => {
      if (!dragging) return
      curX = e.clientX
      setX(curX - startX, false)
      e.stopPropagation()
    }
    const up = () => {
      if (!dragging) return
      dragging = false
      const dx = curX - startX
      if (dx < -40 && page < 1) page++
      else if (dx > 40 && page > 0) page--
      setX(0, true)
    }
    screen.addEventListener('pointerdown', down)
    screen.addEventListener('pointermove', move)
    screen.addEventListener('pointerup', up)
    screen.addEventListener('pointercancel', up)
    setX(0, false)

    return () => {
      screen.removeEventListener('pointerdown', down)
      screen.removeEventListener('pointermove', move)
      screen.removeEventListener('pointerup', up)
      screen.removeEventListener('pointercancel', up)
    }
  }, [])

  const METAL = '#43474e,#26282d 34%,#0c0d10 66%,#33363c'
  // Lighter rail tone, distinct from the front bezel, like a real metal edge
  // against a glass face — brighter at the centre, darker toward front/back,
  // which is what sells a cylindrical, rounded-off edge rather than a flat strip.
  const EDGE_METAL = '#9aa0aa,#3a3f47 30%,#1a1c20 50%,#3a3f47 70%,#9aa0aa'
  const edgeSide = {
    width: EDGE_DEPTH,
    height: '100%' as const,
    marginLeft: -EDGE_DEPTH / 2,
    background: `linear-gradient(90deg,${EDGE_METAL})`,
  }
  const edgeTopBottom = {
    width: '100%' as const,
    height: EDGE_DEPTH,
    marginTop: -EDGE_DEPTH / 2,
    background: `linear-gradient(180deg,${EDGE_METAL})`,
  }

  return (
    <div
      ref={stageRef}
      // Stage must be at least as tall as the phone actually renders at each
      // breakpoint (620px unscaled × the matching scale class below), or the
      // Hero section's overflow-hidden clips the bottom off — the home
      // indicator bar and bottom dock, gone. 480/560 only covered the
      // mobile/tablet scaled-down sizes; lg: renders at full scale (620px)
      // and needs its own taller stage.
      className="relative grid h-[480px] select-none place-items-center sm:h-[560px] lg:h-[640px]"
      style={{ perspective: 1400, perspectiveOrigin: '50% 42%', touchAction: 'pan-y' }}
    >
      <div
        className="pointer-events-none absolute h-[420px] w-[420px] rounded-full blur-[30px]"
        style={{
          background:
            'radial-gradient(circle, color-mix(in srgb, var(--color-brand) 22%, transparent), transparent 65%)',
        }}
      />
      <div ref={wrapRef} className="relative" style={{ transformStyle: 'preserve-3d' }}>
        <div
          ref={phoneRef}
          // `scale-*` utilities only touch X/Y — at the sm/default breakpoints
          // that shrinks the face while every translateZ (edge thickness,
          // camera bump, core panel) stays at full absolute size, throwing the
          // front/edge radius match tuned for the unscaled box out of sync and
          // reopening a gap at the corners. Setting the `scale` property's
          // three-value form scales Z right along with X/Y so the geometry
          // stays proportional at every breakpoint.
          className="relative origin-center [scale:0.72_0.72_0.72] sm:[scale:0.85_0.85_0.85] lg:[scale:1_1_1]"
          style={{
            width: WIDTH,
            height: HEIGHT,
            transformStyle: 'preserve-3d',
            transform: 'rotateX(6deg) rotateY(-22deg)',
          }}
        >
          {/* CORE — a thin backing panel sandwiched between front and back,
              radius set between the front's and the edges' so any residual
              sliver at the seam is only a couple of px off either curve. */}
          <div
            className="absolute inset-0 rounded-[26px]"
            style={{ background: `linear-gradient(160deg,${METAL})` }}
          />

          {/* FRONT */}
          <div
            ref={frontRef}
            className="absolute inset-0 rounded-[30px] p-[6px]"
            style={{
              backfaceVisibility: 'hidden',
              transform: `translateZ(${FACE_Z}px)`,
              background: `linear-gradient(160deg,${METAL})`,
            }}
          >
            <div
              ref={screenRef}
              className="relative h-full w-full overflow-hidden rounded-[24px] bg-black"
              style={{ containerType: 'inline-size', containerName: 'phone', touchAction: 'pan-y' }}
            >
              <div ref={pagesRef} className="flex h-full" style={{ width: '200%', willChange: 'transform' }}>
                <div className="relative h-full" style={{ width: '50%' }}>
                  <PhoneStatusBar light />
                  <HomeScreen />
                </div>
                <div className="relative h-full" style={{ width: '50%' }}>
                  <PhoneStatusBar light={false} />
                  <AppScreen />
                </div>
              </div>
              <div className="pointer-events-none absolute left-1/2 top-[11px] z-20 h-6 w-[86px] -translate-x-1/2 rounded-full bg-black" />
            </div>
          </div>

          {/* BACK — layered glass-back look: a diagonal glossy streak + a soft
              top sheen on top of the base colour, instead of one flat gradient,
              so it reads as reflective glass rather than a painted panel. */}
          <div
            ref={backRef}
            className="absolute inset-0 rounded-[30px]"
            style={{
              backfaceVisibility: 'hidden',
              transform: `translateZ(-${FACE_Z}px) rotateY(180deg)`,
              transformStyle: 'preserve-3d',
              backgroundImage: [
                'linear-gradient(115deg, rgba(255,255,255,.32) 0%, rgba(255,255,255,0) 14%, rgba(255,255,255,0) 46%, rgba(255,255,255,.14) 56%, rgba(255,255,255,0) 72%)',
                'radial-gradient(120% 90% at 28% 0%, rgba(255,255,255,.2), transparent 60%)',
                'linear-gradient(150deg,#24407a,#0b1020 55%,#3a2f8f)',
              ].join(','),
              boxShadow: 'inset 0 0 46px rgba(0,0,0,.45), inset 0 1px 1px rgba(255,255,255,.25)',
              visibility: 'hidden',
            }}
          >
            {/* camera module — translateZ lifts it physically off the back
                panel's own plane so the bump is visible in the side profile,
                not just implied by a drop shadow. */}
            <div
              className="absolute grid grid-cols-2 gap-2 rounded-[26px] p-3"
              style={{
                top: 22,
                left: 22,
                width: 104,
                height: 104,
                transform: 'translateZ(7px)',
                background: 'radial-gradient(circle at 35% 30%,#3a4252,#0a0d16)',
                boxShadow: 'inset 0 1px 2px rgba(255,255,255,.15), inset 0 0 14px rgba(0,0,0,.6), 0 3px 6px rgba(0,0,0,.5)',
              }}
            >
              {[0, 1, 2].map((i) => (
                <span
                  key={i}
                  className="relative rounded-full"
                  style={{
                    background: 'radial-gradient(circle at 38% 32%,#4a5568 0%,#1a202c 45%,#05070c 100%)',
                    boxShadow: 'inset 0 0 0 3px rgba(150,160,175,.55), inset 0 0 10px rgba(0,0,0,.85)',
                  }}
                >
                  <span
                    className="absolute rounded-full"
                    style={{ top: '26%', left: '30%', width: '18%', height: '18%', background: 'rgba(255,255,255,.55)', filter: 'blur(1px)' }}
                  />
                </span>
              ))}
              <span className="grid place-items-center">
                <span className="rounded-full" style={{ width: '55%', height: '55%', background: 'radial-gradient(circle at 40% 35%,#cbd5e1,#64748b)' }} />
              </span>
            </div>
          </div>

          {/* EDGES — full-length, pill-capped so they taper into the front/
              back's rounded corner instead of overhanging it as a hard
              square, and lightened toward the centre of each strip to read
              as a rounded metal rail rather than a flat painted strip. */}
          <div
            className="absolute left-1/2 top-0 rounded-full"
            style={{ ...edgeSide, transform: `rotateY(90deg) translateZ(${HALF_W}px)`, transformStyle: 'preserve-3d' }}
          >
            {/* volume-button bump on the right edge, lifted off the rail's
                own plane the same way the camera module lifts off the back */}
            <div
              className="absolute rounded-[3px]"
              style={{
                top: '18%',
                left: '50%',
                width: 6,
                height: 24,
                transform: 'translate(-50%, 0) translateZ(4px)',
                background: 'linear-gradient(90deg,#c4c9d1,#6b7079)',
                boxShadow: '0 1px 2px rgba(0,0,0,.5)',
              }}
            />
          </div>
          <div
            className="absolute left-1/2 top-0 rounded-full"
            style={{ ...edgeSide, transform: `rotateY(-90deg) translateZ(${HALF_W}px)` }}
          />
          <div
            className="absolute left-0 top-1/2 rounded-full"
            style={{ ...edgeTopBottom, transform: `rotateX(90deg) translateZ(${HALF_H}px)` }}
          />
          <div
            className="absolute left-0 top-1/2 rounded-full"
            style={{ ...edgeTopBottom, transform: `rotateX(-90deg) translateZ(${HALF_H}px)` }}
          />
        </div>
      </div>
    </div>
  )
}
