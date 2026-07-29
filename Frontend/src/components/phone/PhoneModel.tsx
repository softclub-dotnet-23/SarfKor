import { Suspense, useEffect, useRef } from 'react'
import { Canvas, useFrame } from '@react-three/fiber'
import { Center, Environment, Html, useGLTF } from '@react-three/drei'
import PhoneScreen, { SCREEN_PX_WIDTH } from './PhoneScreen'
import { MathUtils } from 'three'
import type { Group } from 'three'

const MODEL_URL = '/models/phone.glb'

// Замеры меша экрана BasePhone_Screen_0 в координатах группы
// (после Center и MODEL_SCALE, до вращения).
const SCREEN_WORLD_WIDTH = 1.3062
const SCREEN_CENTER: [number, number, number] = [-0.0066, 0, -0.1145]

// Экран смотрит в -Z, поэтому HTML приподнимаем в ту же сторону, иначе плоскости
// совпадут и начнут мерцать z-fighting-ом.
const SCREEN_LIFT = 0.006

// drei при transform считает мировую ширину как clientWidth * distanceFactor / 400
// (web/Html.js). Разворачиваем формулу, чтобы интерфейс сел ровно в границы экрана.
const HTML_DISTANCE_FACTOR = (SCREEN_WORLD_WIDTH * 400) / SCREEN_PX_WIDTH

// Габариты модели в файле ~0.15 юнита — при камере на z=5 это меньше 4% кадра.
// Поднимаем до вменяемого размера; крутите это число, если нужен другой кадр.
const MODEL_SCALE = 20

// Насколько быстро модель догоняет скролл. Меньше — тягучее, больше — резче.
const LERP_FACTOR = 0.08

// Базовая ориентация: в файле нормаль экрана смотрит в -Z, камера стоит со
// стороны +Z. Разворот на 180° ставит экран лицом к зрителю — от этого
// значения и отсчитывается всё вращение, а не от нуля.
const BASE_Y = Math.PI

console.log('[PhoneModel] BASE_Y (экран анфас) =', BASE_Y, 'рад =', MathUtils.radToDeg(BASE_Y), '°')

// Ключевые кадры вращения по Y — смещения относительно BASE_Y.
// Диапазон +0.7..-0.8 рад лежит внутри ±π/2, поэтому корпус не успевает
// повернуться к зрителю задней крышкой ни в одной точке скролла.
const ROTATION_Y_KEYS: ReadonlyArray<readonly [number, number]> = [
  [0.0, 0.7],
  [0.3, 0.1],
  [0.6, -0.35],
  [1.0, -0.8],
]

/** Кусочно-линейная интерполяция по ключевым кадрам. */
function rotationYAt(p: number) {
  for (let i = 0; i < ROTATION_Y_KEYS.length - 1; i++) {
    const [fromP, fromV] = ROTATION_Y_KEYS[i]
    const [toP, toV] = ROTATION_Y_KEYS[i + 1]

    if (p <= toP) {
      const t = MathUtils.clamp((p - fromP) / (toP - fromP), 0, 1)
      return BASE_Y + MathUtils.lerp(fromV, toV, t)
    }
  }

  return BASE_Y + ROTATION_Y_KEYS[ROTATION_Y_KEYS.length - 1][1]
}

/** Прогресс скролла страницы от 0 до 1, живёт в ref — без ре-рендеров. */
function useScrollProgress() {
  const progress = useRef(0)

  useEffect(() => {
    const update = () => {
      const scrollable = document.body.scrollHeight - window.innerHeight
      const raw = scrollable > 0 ? window.scrollY / scrollable : 0
      progress.current = MathUtils.clamp(raw, 0, 1)
    }

    update()
    window.addEventListener('scroll', update, { passive: true })
    window.addEventListener('resize', update, { passive: true })

    return () => {
      window.removeEventListener('scroll', update)
      window.removeEventListener('resize', update)
    }
  }, [])

  return progress
}

/** Масштаб: 0.8 в начале → 1.2 к середине → 0.9 в конце. */
function scaleAt(p: number) {
  return p < 0.5
    ? MathUtils.lerp(0.8, 1.2, p / 0.5)
    : MathUtils.lerp(1.2, 0.9, (p - 0.5) / 0.5)
}

function Phone({ progress }: { progress: React.RefObject<number> }) {
  const { scene } = useGLTF(MODEL_URL)
  const group = useRef<Group>(null)

  useFrame(() => {
    const g = group.current
    if (!g) return

    const p = progress.current

    // Считаем целевые значения по прогрессу, затем догоняем их через lerp —
    // отсюда инерция: модель тянется за скроллом, а не прыгает за ним.
    const targetRotationY = rotationYAt(p)
    const targetRotationX = MathUtils.lerp(0.12, -0.12, p)
    const targetPositionX = MathUtils.lerp(2, -1.5, p)
    const targetPositionY = MathUtils.lerp(-0.5, 0.5, p)
    const targetScale = scaleAt(p)

    g.rotation.y = MathUtils.lerp(g.rotation.y, targetRotationY, LERP_FACTOR)
    g.rotation.x = MathUtils.lerp(g.rotation.x, targetRotationX, LERP_FACTOR)
    g.position.x = MathUtils.lerp(g.position.x, targetPositionX, LERP_FACTOR)
    g.position.y = MathUtils.lerp(g.position.y, targetPositionY, LERP_FACTOR)

    const nextScale = MathUtils.lerp(g.scale.x, targetScale, LERP_FACTOR)
    g.scale.setScalar(nextScale)
  })

  return (
    // Стартуем сразу в нужной ориентации: иначе lerp поедет от нуля и на
    // первых кадрах мелькнёт задняя крышка.
    <group ref={group} rotation={[0, rotationYAt(0), 0]}>
      <Center>
        <primitive object={scene} scale={MODEL_SCALE} />
      </Center>

      {/* Лежит в той же группе, что и модель, поэтому наследует её поворот,
          позицию и масштаб — интерфейс приклеен к экрану, а не догоняет его.
          Разворот на π нужен потому, что плоскость Html по умолчанию смотрит
          в +Z, а экран модели — в -Z. */}
      <Html
        transform
        occlude="blending"
        distanceFactor={HTML_DISTANCE_FACTOR}
        position={[SCREEN_CENTER[0], SCREEN_CENTER[1], SCREEN_CENTER[2] - SCREEN_LIFT]}
        rotation={[0, Math.PI, 0]}
      >
        <PhoneScreen />
      </Html>
    </group>
  )
}

export default function PhoneModel() {
  const progress = useScrollProgress()

  return (
    <div className="pointer-events-none fixed left-0 top-0 z-[1] h-screen w-screen">
      <Canvas camera={{ position: [0, 0, 5], fov: 45 }}>
        <ambientLight intensity={0.6} />
        <directionalLight position={[5, 5, 5]} intensity={2} />

        <Suspense fallback={null}>
          <Phone progress={progress} />
          <Environment preset="city" />
        </Suspense>
      </Canvas>
    </div>
  )
}

useGLTF.preload(MODEL_URL)
