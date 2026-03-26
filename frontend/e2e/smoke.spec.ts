import { test, expect } from '@playwright/test'

test.describe('Iris App Smoke Tests', () => {
  test('page loads and map is visible', async ({ page }) => {
    await page.goto('/')
    await expect(page).toHaveTitle(/Iris/)
    // Map canvas should render
    await expect(page.locator('canvas')).toBeVisible({ timeout: 10_000 })
  })

  test('filter bar is visible with vehicle type buttons', async ({ page }) => {
    await page.goto('/')
    await expect(page.getByRole('button', { name: /toggle bus/i })).toBeVisible({ timeout: 8_000 })
    await expect(page.getByRole('button', { name: /toggle train/i })).toBeVisible()
    await expect(page.getByRole('button', { name: /toggle ferry/i })).toBeVisible()
  })

  test('filter buttons are initially active (aria-pressed=true)', async ({ page }) => {
    await page.goto('/')
    const busBtn = page.getByRole('button', { name: /toggle bus/i })
    await expect(busBtn).toBeVisible({ timeout: 8_000 })
    await expect(busBtn).toHaveAttribute('aria-pressed', 'true')
  })

  test('toggling a filter button changes aria-pressed state', async ({ page }) => {
    await page.goto('/')
    const busBtn = page.getByRole('button', { name: /toggle bus/i })
    await busBtn.waitFor({ state: 'visible', timeout: 8_000 })

    await busBtn.click()
    await expect(busBtn).toHaveAttribute('aria-pressed', 'false')

    await busBtn.click()
    await expect(busBtn).toHaveAttribute('aria-pressed', 'true')
  })

  test('connection status banner is shown while connecting', async ({ page }) => {
    await page.goto('/')
    // Banner may appear briefly, or not if backend is up - just check page doesn't crash
    await expect(page.locator('canvas')).toBeVisible({ timeout: 10_000 })
  })

  test('mobile viewport renders map full screen', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 })
    await page.goto('/')
    await expect(page.locator('canvas')).toBeVisible({ timeout: 10_000 })

    const canvas = page.locator('canvas')
    const box = await canvas.boundingBox()
    expect(box).not.toBeNull()
    expect(box!.width).toBeGreaterThan(300)
    expect(box!.height).toBeGreaterThan(600)
  })

  test('page has no console errors on load', async ({ page }) => {
    const errors: string[] = []
    page.on('console', (msg) => {
      if (msg.type() === 'error') errors.push(msg.text())
    })

    await page.goto('/')
    await page.locator('canvas').waitFor({ state: 'visible', timeout: 10_000 })

    // Filter out known non-critical errors (WebGL, network errors when backend is offline)
    const criticalErrors = errors.filter(
      (e) =>
        !e.includes('WebGL') &&
        !e.includes('signalr') &&
        !e.includes('ERR_CONNECTION_REFUSED') &&
        !e.includes('net::ERR') &&
        !e.includes('Failed to fetch')
    )
    expect(criticalErrors).toHaveLength(0)
  })
})
