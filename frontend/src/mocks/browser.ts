/**
 * MSW Browser Setup
 * Configures Mock Service Worker for the browser
 */

import { setupWorker } from 'msw/browser'
import { handlers } from './handlers'

export const worker = setupWorker(...handlers)
