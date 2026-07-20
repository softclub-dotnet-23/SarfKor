import { useState } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import { Eyebrow } from './Eyebrow'
import { ChevronDownIcon } from './icons'
import { LogoMark } from './Logo'

const FAQS = [
  {
    question: 'Как работает сканирование?',
    answer:
      'Наведите камеру телефона на штрихкод товара — приложение мгновенно распознает его и покажет цены во всех ближайших магазинах.',
  },
  {
    question: 'Приложение бесплатное?',
    answer:
      'Да, Sarfkor полностью бесплатен для покупателей. Мы зарабатываем на партнёрских кабинетах для магазинов, а не на пользователях.',
  },
  {
    question: 'Как часто обновляются цены?',
    answer:
      'Цены обновляются в реальном времени: их вносят сами магазины-партнёры и подтверждают пользователи через систему репутации, поэтому данные остаются свежими каждый день.',
  },
  {
    question: 'Можно ли добавить свой магазин?',
    answer:
      'Конечно. Откройте партнёрский кабинет, добавьте магазин и загрузите цены — уже через несколько минут его увидят покупатели поблизости.',
  },
]

function FaqDecoration() {
  return (
    <div className="relative mx-auto hidden h-56 w-56 lg:block">
      <motion.div
        initial={{ opacity: 0, y: 20, rotate: -8 }}
        whileInView={{ opacity: 1, y: 0, rotate: -8 }}
        viewport={{ once: true }}
        transition={{ duration: 0.6 }}
        className="animate-float absolute left-2 top-6 h-28 w-28 rounded-3xl shadow-[var(--shadow-lift)]"
        style={{ background: 'linear-gradient(135deg,#4C8CFF,#2F6FEB)' }}
      >
        <div className="grid h-full place-items-center">
          <LogoMark size={44} />
        </div>
      </motion.div>
      <motion.div
        initial={{ opacity: 0, y: 20, rotate: 10 }}
        whileInView={{ opacity: 1, y: 0, rotate: 10 }}
        viewport={{ once: true }}
        transition={{ duration: 0.6, delay: 0.15 }}
        className="absolute bottom-4 right-2 grid h-24 w-24 place-items-center rounded-3xl bg-[color:var(--bg-card)] text-4xl font-bold text-[color:var(--color-brand)] shadow-[var(--shadow-lift)] ring-1 ring-[color:var(--border-subtle)]"
        style={{ animation: 'float 6s ease-in-out infinite', animationDelay: '1.2s' }}
      >
        ?
      </motion.div>
    </div>
  )
}

function FaqItem({
  question,
  answer,
  open,
  onToggle,
}: {
  question: string
  answer: string
  open: boolean
  onToggle: () => void
}) {
  return (
    <div className="border-b border-[color:var(--border-subtle)] last:border-none">
      <button
        onClick={onToggle}
        className="flex w-full items-center justify-between gap-4 py-5 text-left"
        aria-expanded={open}
      >
        <span
          className={`text-[16px] transition-colors ${
            open ? 'font-semibold text-[color:var(--text-primary)]' : 'font-medium text-[color:var(--text-primary)]/90'
          }`}
        >
          {question}
        </span>
        <motion.span
          animate={{ rotate: open ? 180 : 0 }}
          transition={{ duration: 0.3 }}
          className={`shrink-0 rounded-full p-1.5 ${
            open ? 'bg-[color:var(--color-brand)] text-white' : 'text-[color:var(--text-secondary)]'
          }`}
        >
          <ChevronDownIcon width={16} height={16} />
        </motion.span>
      </button>
      <AnimatePresence initial={false}>
        {open && (
          <motion.div
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: 'auto', opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.3, ease: [0.16, 1, 0.3, 1] }}
            className="overflow-hidden"
          >
            <p className="pb-5 pr-8 text-[15px] leading-relaxed text-[color:var(--text-secondary)]">
              {answer}
            </p>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}

export function FAQ() {
  const [openIndex, setOpenIndex] = useState(2)

  return (
    <section id="faq" className="py-24 lg:py-32">
      <div className="mx-auto max-w-7xl px-6 lg:px-10">
        <div className="grid grid-cols-1 gap-12 lg:grid-cols-[0.8fr_1.2fr] lg:gap-16">
          <div>
            <Eyebrow align="left">Частые вопросы</Eyebrow>
            <motion.h2
              initial={{ opacity: 0, y: 16 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true, margin: '-80px' }}
              transition={{ duration: 0.6 }}
              className="text-3xl font-bold tracking-tight text-[color:var(--text-primary)] sm:text-4xl"
            >
              Ответы на вопросы
            </motion.h2>
            <FaqDecoration />
          </div>

          <motion.div
            initial={{ opacity: 0, y: 20 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, margin: '-60px' }}
            transition={{ duration: 0.6 }}
            className="rounded-[28px] bg-[color:var(--bg-card)] px-6 shadow-[var(--shadow-soft)] ring-1 ring-[color:var(--border-subtle)] sm:px-8"
          >
            {FAQS.map((faq, i) => (
              <FaqItem
                key={faq.question}
                question={faq.question}
                answer={faq.answer}
                open={openIndex === i}
                onToggle={() => setOpenIndex((cur) => (cur === i ? -1 : i))}
              />
            ))}
          </motion.div>
        </div>
      </div>
    </section>
  )
}
