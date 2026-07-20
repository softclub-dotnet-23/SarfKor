import { Header } from './components/Header'
import { Hero } from './components/Hero'
import { HowItWorks } from './components/HowItWorks'
import { StatsBar } from './components/StatsBar'
import { StoresMap } from './components/StoresMap'
import { Testimonials } from './components/Testimonials'
import { FAQ } from './components/FAQ'
import { Footer } from './components/Footer'
import { SectionDots } from './components/SectionDots'

function App() {
  return (
    <div className="min-h-screen bg-[color:var(--bg-app)]">
      <Header />
      <SectionDots />

      <main>
        <Hero />
        <HowItWorks />
        <section id="stats" className="py-24 lg:py-32">
          <StatsBar />
        </section>
        <StoresMap />
        <Testimonials />
        <FAQ />
      </main>

      <Footer />
    </div>
  )
}

export default App
