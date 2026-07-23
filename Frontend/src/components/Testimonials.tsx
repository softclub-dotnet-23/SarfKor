import { useEffect, useMemo, useState } from 'react'
import { motion } from 'framer-motion'
import { Eyebrow } from './Eyebrow'
import { ChevronLeftIcon, ChevronRightIcon, StarIcon } from './icons'
import { usePerPage } from '../hooks/usePerPage'

interface Testimonial {
  name: string
  color: string
  text: string
}

const TESTIMONIALS: Testimonial[] = [
  {
    name: 'Мария Т.',
    color: '#FF9F6B',
    text: 'Отличное приложение! Экономлю каждый день на покупках для семьи. Очень удобно и быстро.',
  },
  {
    name: 'Сурдоб Х.',
    color: '#6BA4FF',
    text: 'Теперь точно знаю где дешевле. Sarfkor стал моим помощником №1 в походах за продуктами.',
  },
  {
    name: 'Зарнигор А.',
    color: '#FF6B9A',
    text: 'Функция сравнения чека просто супер! Уже несколько раз ловила расхождение с ценником.',
  },
  {
    name: 'Феруз М.',
    color: '#6BCB9E',
    text: 'Экономит и деньги, и время. Рекомендую всем, кто хочет покупать умнее каждый день.',
  },
  {
    name: 'Далер Н.',
    color: '#FFC96B',
    text: 'Использую перед каждым походом в магазин — экономия ощутима уже через месяц.',
  },
  {
    name: 'Нигина С.',
    color: '#B08BFF',
    text: 'Простой и понятный интерфейс, супер быстро находит товар по штрихкоду.',
  },
]

function chunk<T>(arr: T[], size: number): T[][] {
  const result: T[][] = []
  for (let i = 0; i < arr.length; i += size) result.push(arr.slice(i, i + size))
  return result
}

function TestimonialCard({ testimonial }: { testimonial: Testimonial }) {
  return (
    <div className="flex h-full flex-col rounded-3xl bg-[color:var(--bg-card)] p-6 shadow-[var(--shadow-soft)] ring-1 ring-[color:var(--border-subtle)]">
      <div className="flex items-center gap-3">
        <span
          className="grid h-11 w-11 shrink-0 place-items-center rounded-full text-sm font-bold text-white"
          style={{ background: `linear-gradient(135deg, ${testimonial.color}, ${testimonial.color}bb)` }}
        >
          {testimonial.name.charAt(0)}
        </span>
        <div>
          <p className="font-semibold text-[color:var(--text-primary)]">{testimonial.name}</p>
          <div className="mt-0.5 flex gap-0.5 text-amber-400">
            {Array.from({ length: 5 }).map((_, i) => (
              <StarIcon key={i} width={13} height={13} />
            ))}
          </div>
        </div>
      </div>
      <p className="mt-4 text-[15px] leading-relaxed text-[color:var(--text-secondary)]">
        {testimonial.text}
      </p>
    </div>
  )
}

export function Testimonials() {
  const perPage = usePerPage()
  const pages = useMemo(() => chunk(TESTIMONIALS, perPage), [perPage])
  const [index, setIndex] = useState(0)
  const [paused, setPaused] = useState(false)

  useEffect(() => {
    if (index > pages.length - 1) setIndex(0)
  }, [pages, index])

  useEffect(() => {
    if (paused || pages.length <= 1) return
    const id = setInterval(() => setIndex((i) => (i + 1) % pages.length), 5000)
    return () => clearInterval(id)
  }, [paused, pages.length])

  const goTo = (i: number) => setIndex((i + pages.length) % pages.length)

  return (
    <section id="testimonials" className="py-24 lg:py-32">
      <div className="mx-auto max-w-7xl px-6 lg:px-10">
        <div className="text-center">
          <Eyebrow>Что говорят наши пользователи</Eyebrow>
          <motion.h2
            initial={{ opacity: 0, y: 16 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, margin: '-80px' }}
            transition={{ duration: 0.6 }}
            className="text-3xl font-bold tracking-tight text-[color:var(--text-primary)] sm:text-4xl"
          >
            Реальные отзывы реальных людей
          </motion.h2>
        </div>

        <div
          className="relative mt-14"
          onMouseEnter={() => setPaused(true)}
          onMouseLeave={() => setPaused(false)}
        >
          <button
            onClick={() => goTo(index - 1)}
            aria-label="Предыдущие отзывы"
            className="absolute left-0 top-1/2 z-10 hidden h-11 w-11 -translate-x-5 -translate-y-1/2 place-items-center rounded-full bg-[color:var(--bg-card)] text-[color:var(--text-primary)] shadow-[var(--shadow-soft)] ring-1 ring-[color:var(--border-subtle)] transition-transform hover:scale-105 sm:grid"
          >
            <ChevronLeftIcon width={18} height={18} />
          </button>
          <button
            onClick={() => goTo(index + 1)}
            aria-label="Следующие отзывы"
            className="absolute right-0 top-1/2 z-10 hidden h-11 w-11 -translate-y-1/2 translate-x-5 place-items-center rounded-full bg-[color:var(--bg-card)] text-[color:var(--text-primary)] shadow-[var(--shadow-soft)] ring-1 ring-[color:var(--border-subtle)] transition-transform hover:scale-105 sm:grid"
          >
            <ChevronRightIcon width={18} height={18} />
          </button>

          <div className="overflow-hidden">
            <motion.div
              className="flex"
              animate={{ x: `-${index * 100}%` }}
              transition={{ type: 'spring', stiffness: 300, damping: 32 }}
            >
              {pages.map((page, pageIndex) => (
                <div key={pageIndex} className="grid w-full shrink-0 grid-cols-1 gap-5 px-1 sm:grid-cols-2 lg:grid-cols-4">
                  {page.map((testimonial) => (
                    <TestimonialCard key={testimonial.name} testimonial={testimonial} />
                  ))}
                </div>
              ))}
            </motion.div>
          </div>
        </div>

        {pages.length > 1 && (
          <div className="mt-8 flex justify-center gap-2">
            {pages.map((_, i) => (
              <button
                key={i}
                onClick={() => goTo(i)}
                aria-label={`Страница отзывов ${i + 1}`}
                className={
                  i === index
                    ? 'h-2 w-6 rounded-full bg-[color:var(--color-brand)] transition-all'
                    : 'h-2 w-2 rounded-full bg-[color:var(--text-secondary)]/30 transition-all hover:bg-[color:var(--text-secondary)]/50'
                }
              />
            ))}
          </div>
        )}
      </div>
    </section>
  )
}
