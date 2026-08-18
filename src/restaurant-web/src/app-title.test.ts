import { expect, test } from 'vitest'
import { appTitle } from './app-title'

test('exposes the application title', () => {
  expect(appTitle).toBe('Restaurant Handbook')
})
