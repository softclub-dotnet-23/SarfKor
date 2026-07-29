import { useEffect, useRef } from 'react'
import { MathUtils } from 'three'
import ScreenLaunch from './screens/ScreenLaunch'
import ScreenScan from './screens/ScreenScan'
import ScreenResults from './screens/ScreenResults'
import ScreenSavings from './screens/ScreenSavings'

// Логический размер интерфейса в пикселях. Соотношение 390:844 = 1:2.164 —
// ровно как у меша экрана, поэтому картинка ложится без полей и растяжки.
export const SCREEN_PX_WIDTH = 390
export const SCREEN_PX_HEIGHT = 844

type Fade = readonly [number, number] | null

type ScreenStep = {
  Component: () => React.JSX.Element
  /** Диапазон прогресса, на котором экран проявляется. null — виден с самого начала. */
  fadeIn: Fade
  /** Диапазон, на котором гаснет. null — остаётся до конца скролла. */
  fadeOut: Fade
}

const SCREEN_STEPS: ScreenStep[] = [
  { Component: ScreenLaunch, fadeIn: null, fadeOut: [0.2, 0.27] },
  { Component: ScreenScan, fadeIn: [0.2, 0.27], fadeOut: [0.45, 0.52] },
  { Component: ScreenResults, fadeIn: [0.45, 0.52], fadeOut: [0.7, 0.77] },
  { Component: ScreenSavings, fadeIn: [0.7, 0.77], fadeOut: null },
]

/**
 * Непрозрачность экрана при заданном прогрессе. smoothstep вместо линейной
 * интерполяции — на середине перехода оба соседних экрана дают по ~0.5,
 * поэтому кроссфейд читается плавно, без провала в темноту.
 */
function opacityAt(progress: number, step: ScreenStep) {
  const appearing = step.fadeIn ? MathUtils.smoothstep(progress, step.fadeIn[0], step.fadeIn[1]) : 1
  const leaving = step.fadeOut ? 1 - MathUtils.smoothstep(progress, step.fadeOut[0], step.fadeOut[1]) : 1

  return MathUtils.clamp(Math.min(appearing, leaving), 0, 1)
}

export default function PhoneScreen({ progress }: { progress: React.RefObject<number> }) {
  const layers = useRef<Array<HTMLDivElement | null>>([])

  // Крутим свой rAF и пишем opacity напрямую в DOM: тот же ref, что и у поворота
  // модели, поэтому экраны меняются с ним синхронно и без ре-рендеров React.
  useEffect(() => {
    let frame = 0

    const tick = () => {
      const p = progress.current

      for (let i = 0; i < SCREEN_STEPS.length; i++) {
        const layer = layers.current[i]
        if (layer) layer.style.opacity = String(opacityAt(p, SCREEN_STEPS[i]))
      }

      frame = requestAnimationFrame(tick)
    }

    frame = requestAnimationFrame(tick)
    return () => cancelAnimationFrame(frame)
  }, [progress])

  return (
    <div
      className="relative select-none overflow-hidden bg-[#0a0a0a]"
      style={{ width: SCREEN_PX_WIDTH, height: SCREEN_PX_HEIGHT }}
    >
      {SCREEN_STEPS.map((step, i) => (
        <div
          key={i}
          ref={(el) => {
            layers.current[i] = el
          }}
          className="absolute inset-0"
          // Стартовое значение, чтобы первый кадр не мигнул всеми экранами разом.
          style={{ opacity: opacityAt(0, step) }}
        >
          <step.Component />
        </div>
      ))}
    </div>
  )
}
