import { useEffect, useRef, type ReactNode } from 'react'
import { PhoneFrame } from './PhoneFrame'
import { Eyebrow } from './Eyebrow'
import { ScanScreen, ProductInfoScreen, CompareScreen, SavingsScreen } from './howitworks/StepScreens'

interface StepData {
  tag: string
  title: string
  description: string
  feats: string[]
  accent: string
  num: string
  screen: ReactNode
  statusLight: boolean
}

const STEPS: StepData[] = [
  {
    tag: 'Шаг 01',
    title: 'Сканируй штрихкод',
    description: 'Наведи камеру на товар — Sarfkor мгновенно распознаёт штрихкод и находит товар в базе.',
    feats: ['Работает даже при плохом свете', 'Распознавание за 0,3 секунды'],
    accent: '#2563eb',
    num: '01',
    screen: <ScanScreen />,
    statusLight: true,
  },
  {
    tag: 'Шаг 02',
    title: 'Получи информацию',
    description: 'Мы находим товар в базе и показываем полную карточку: состав, объём и цену за единицу.',
    feats: ['Честная цена за 1л / 100г', 'История изменения цены'],
    accent: '#7c3aed',
    num: '02',
    screen: <ProductInfoScreen />,
    statusLight: false,
  },
  {
    tag: 'Шаг 03',
    title: 'Сравни цены',
    description: 'Список всех магазинов рядом с ценами — от самой выгодной до самой дорогой.',
    feats: ['Сортировка по расстоянию', 'Отметка «лучшая цена»'],
    accent: '#0891b2',
    num: '03',
    screen: <CompareScreen />,
    statusLight: false,
  },
  {
    tag: 'Шаг 04',
    title: 'Экономь деньги',
    description: 'Выбирай лучшее предложение и строй маршрут до магазина в один тап.',
    feats: ['Маршрут в один тап', 'Учёт сэкономленного за месяц'],
    accent: '#12b76a',
    num: '04',
    screen: <SavingsScreen />,
    statusLight: false,
  },
]

const N = STEPS.length

function easeInOutCubic(x: number) {
  return x < 0.5 ? 4 * x * x * x : 1 - Math.pow(-2 * x + 2, 3) / 2
}

function FeatRow({ text, accent }: { text: string; accent: string }) {
  return (
    <div className="flex items-center gap-3 text-[15px] font-semibold text-[color:var(--text-primary)]">
      <span
        className="grid h-[26px] w-[26px] shrink-0 place-items-center rounded-[9px]"
        style={{ background: `color-mix(in srgb, ${accent} 15%, transparent)`, color: accent }}
      >
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={3} strokeLinecap="round" strokeLinejoin="round">
          <path d="M20 6 9 17l-5-5" />
        </svg>
      </span>
      {text}
    </div>
  )
}

export function HowItWorks() {
  const sectionRef = useRef<HTMLDivElement>(null)
  const ghostRef = useRef<HTMLDivElement>(null)
  const rotRefs = useRef<(HTMLDivElement | null)[]>([])
  const shadowRefs = useRef<(HTMLDivElement | null)[]>([])
  const textRefs = useRef<(HTMLDivElement | null)[]>([])
  const sceneRefs = useRef<(HTMLDivElement | null)[]>([])
  const dotRefs = useRef<(HTMLSpanElement | null)[]>([])

  useEffect(() => {
    const section = sectionRef.current
    const ghost = ghostRef.current
    if (!section || !ghost) return

    let cur = 0
    let raf = 0

    // This loop used to run every frame for the page's entire lifetime, even
    // scrolled miles away from the section — pure wasted main-thread work
    // that competed with anything else happening on the page (including the
    // theme-toggle transition). Only run it while the section is actually
    // near the viewport.
    const frame = () => {
      const rect = section.getBoundingClientRect()
      const total = section.offsetHeight - window.innerHeight
      const target = Math.max(0, Math.min(1, -rect.top / total))
      // 0.1 lagged visibly behind fast real-world scroll input (wheel flicks,
      // trackpad swipes) — by the time it caught up, the next scene was
      // already scrolled past, so outgoing/incoming scenes stayed overlapped
      // far longer than intended. 0.22 tracks the actual scroll position
      // closely while still smoothing out per-frame jitter.
      cur += (target - cur) * 0.22
      const seg = cur * N
      const ghostI = Math.min(N - 1, Math.max(0, Math.floor(seg + 0.28)))

      for (let i = 0; i < N; i++) {
        const side = i % 2 === 0 ? 1 : -1
        const local = seg - i
        const rot = rotRefs.current[i]
        const shadow = shadowRefs.current[i]
        const text = textRefs.current[i]
        const scene = sceneRefs.current[i]
        // Position races ahead of opacity (quad ease-out vs cubic ease-in-out) so that
        // whenever a scene is partially visible during a crossfade, it is already well
        // clear of center — this is what keeps overlapping scenes from visually colliding.
        let op: number
        let txP: number
        let txT: number
        if (local <= 0) {
          const t = Math.max(0, Math.min(1, (local + 0.6) / 0.6))
          const posE = 1 - (1 - t) * (1 - t)
          op = easeInOutCubic(t)
          txP = side * (1 - posE) * 105
          txT = -side * (1 - posE) * 190
        } else if (local < 0.74) {
          op = 1
          txP = 0
          txT = 0
        } else {
          const t = Math.max(0, Math.min(1, (local - 0.74) / 0.6))
          const posE = 1 - (1 - t) * (1 - t)
          op = 1 - easeInOutCubic(t)
          txP = -side * posE * 105
          txT = side * posE * 190
        }
        let rotY = 13 - ((local + 0.6) / 1.94) * 26
        rotY = Math.max(-15, Math.min(15, rotY))

        if (scene) {
          scene.style.zIndex = i === ghostI ? '4' : '3'
          scene.style.pointerEvents = i === ghostI ? 'auto' : 'none'
        }
        if (rot) {
          rot.style.opacity = String(op)
          rot.style.transform = `translateX(${txP.toFixed(1)}%) rotateY(${rotY.toFixed(1)}deg) rotateX(5deg) scale(${(0.9 + 0.1 * op).toFixed(3)})`
        }
        if (shadow) shadow.style.opacity = (op * 0.85).toFixed(2)
        if (text) {
          text.style.opacity = String(op)
          text.style.transform = `translateX(${txT.toFixed(1)}px)`
        }
      }

      ghost.textContent = STEPS[ghostI].num
      ghost.style.color = `color-mix(in srgb, ${STEPS[ghostI].accent} 8%, transparent)`
      dotRefs.current.forEach((dot, i) => {
        if (!dot) return
        dot.style.background = i <= ghostI ? STEPS[ghostI].accent : 'var(--border-subtle)'
        dot.style.width = i === ghostI ? '54px' : '44px'
      })

      raf = requestAnimationFrame(frame)
    }

    let running = false
    const io = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting && !running) {
          running = true
          frame()
        } else if (!entry.isIntersecting && running) {
          running = false
          cancelAnimationFrame(raf)
        }
      },
      { rootMargin: '50% 0px' },
    )
    io.observe(section)

    return () => {
      io.disconnect()
      cancelAnimationFrame(raf)
    }
  }, [])

  return (
    <section
      id="how-it-works"
      ref={sectionRef}
      className="relative bg-[color:var(--bg-section)]"
      style={{ height: '500vh' }}
    >
      <div className="sticky top-0 h-screen overflow-hidden">
        <div className="pointer-events-none absolute inset-x-0 top-[clamp(80px,12vh,112px)] z-[5] text-center">
          <Eyebrow>Как это работает</Eyebrow>
          <h2 className="mt-2 text-[clamp(24px,3.4vw,50px)] font-extrabold tracking-tight text-[color:var(--text-primary)]">
            Всего 4 шага до выгоды
          </h2>
        </div>

        <div
          ref={ghostRef}
          className="pointer-events-none absolute left-[clamp(10px,4vw,80px)] top-1/2 z-[1] -translate-y-1/2 select-none text-[clamp(120px,30vw,400px)] font-extrabold leading-[0.8] tracking-tighter"
        >
          01
        </div>

        <div className="absolute inset-0 z-[3]">
          {STEPS.map((step, i) => (
            <div
              key={step.title}
              ref={(el) => {
                sceneRefs.current[i] = el
              }}
              className="absolute inset-0 grid grid-cols-2 items-center gap-x-[clamp(12px,4vw,72px)] px-[clamp(14px,6vw,100px)] pb-[84px] pt-[clamp(190px,27vh,240px)]"
            >
              <div
                ref={(el) => {
                  textRefs.current[i] = el
                }}
                className={`max-w-[clamp(200px,38vw,440px)] ${
                  i % 2 === 1 ? 'order-2 justify-self-start' : 'order-1 justify-self-end'
                }`}
              >
                <div
                  className="inline-flex items-center gap-2 rounded-full px-4 py-2 text-[13px] font-extrabold"
                  style={{ background: `color-mix(in srgb, ${step.accent} 12%, transparent)`, color: step.accent }}
                >
                  {step.tag}
                </div>
                <h3 className="mt-5 text-[clamp(24px,3.4vw,50px)] font-extrabold leading-[1.02] tracking-tight text-[color:var(--text-primary)]">
                  {step.title}
                </h3>
                <p className="mt-4 text-[clamp(13px,1.3vw,19px)] leading-relaxed text-[color:var(--text-secondary)]">
                  {step.description}
                </p>
                <div className="mt-6 hidden flex-col gap-3 sm:flex">
                  {step.feats.map((feat) => (
                    <FeatRow key={feat} text={feat} accent={step.accent} />
                  ))}
                </div>
              </div>

              <div
                className={`relative grid place-items-center ${
                  i % 2 === 1 ? 'order-1 justify-self-end' : 'order-2 justify-self-start'
                }`}
                style={{ perspective: 1300 }}
              >
                <div
                  ref={(el) => {
                    shadowRefs.current[i] = el
                  }}
                  className="absolute bottom-[2%] left-1/2 h-10 w-2/3 -translate-x-1/2 rounded-full blur-[13px]"
                  style={{ background: 'radial-gradient(ellipse, rgba(11,15,25,.32), transparent 70%)' }}
                />
                <div
                  ref={(el) => {
                    rotRefs.current[i] = el
                  }}
                  className="relative w-[clamp(110px,30vw,220px)]"
                  style={{ transformStyle: 'preserve-3d' }}
                >
                  <PhoneFrame statusBarLight={step.statusLight}>{step.screen}</PhoneFrame>
                </div>
              </div>
            </div>
          ))}
        </div>

        <div className="absolute bottom-[clamp(20px,5vh,52px)] left-1/2 z-[6] flex -translate-x-1/2 gap-3">
          {STEPS.map((_, i) => (
            <span
              key={i}
              ref={(el) => {
                dotRefs.current[i] = el
              }}
              className="h-[5px] w-11 rounded-full bg-[color:var(--border-subtle)] transition-[background] duration-300"
            />
          ))}
        </div>
      </div>
    </section>
  )
}
