import type { ClassValue } from 'clsx'
import { clsx } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

function camelToSnake(str: string): string {
  return str.replace(/[A-Z]/g, letter => `_${letter.toLowerCase()}`)
}

function snakeToCamel(str: string): string {
  return str.replace(/_([a-z0-9])/g, (_, letter) => letter.toUpperCase())
}

export class CaseConverter {
  static toCamelCase<T>(obj: unknown): T {
    if (Array.isArray(obj)) {
      return obj.map(item => CaseConverter.toCamelCase(item)) as unknown as T
    }
    if (obj !== null && typeof obj === 'object' && !(obj instanceof Date)) {
      return Object.keys(obj).reduce((acc, key) => {
        const camelKey = snakeToCamel(key)
        acc[camelKey] = CaseConverter.toCamelCase((obj as Record<string, unknown>)[key])
        return acc
      }, {} as Record<string, unknown>) as T
    }
    return obj as T
  }

  static toSnakeCase<T>(obj: unknown): T {
    if (Array.isArray(obj)) {
      return obj.map(item => CaseConverter.toSnakeCase(item)) as unknown as T
    }
    if (
      obj !== null
      && typeof obj === 'object'
      && !(obj instanceof Date)
      && !(obj instanceof Blob)
      && !(typeof FormData !== 'undefined' && obj instanceof FormData)
    ) {
      return Object.keys(obj).reduce((acc, key) => {
        const snakeKey = camelToSnake(key)
        acc[snakeKey] = CaseConverter.toSnakeCase((obj as Record<string, unknown>)[key])
        return acc
      }, {} as Record<string, unknown>) as T
    }
    return obj as T
  }
}
