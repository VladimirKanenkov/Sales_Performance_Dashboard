import { motion, useSpring, useTransform } from 'framer-motion'
import { useEffect } from 'react'

type Props = {
  value: number
  format: (n: number) => string
  className?: string
}

/**
 * Плавно доводит число до нового значения.
 *
 * @param value - Целевое число.
 * @param format - Форматтер отображения.
 * @param className - Классы обёртки.
 */
export function AnimatedNumber({ value, format, className }: Props) {
  const spring = useSpring(value, { stiffness: 80, damping: 20 })
  const display = useTransform(spring, (v) => format(v))

  useEffect(() => {
    spring.set(value)
  }, [spring, value])

  return <motion.span className={className}>{display}</motion.span>
}
