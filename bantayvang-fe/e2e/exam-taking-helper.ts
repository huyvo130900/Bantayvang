import type { Page } from '@playwright/test'

/**
 * exam-taking-page.tsx gates the whole screen behind a "bat che do toan man hinh" overlay
 * until the browser Fullscreen API resolves. Checking `count() > 0` immediately after
 * navigation races the overlay's own render, so wait for it explicitly instead - and retry the
 * click, since a Fullscreen API request issued from an automated browser occasionally needs a
 * second genuine click before it resolves.
 */
export async function dismissFullscreenGateIfPresent(page: Page) {
  const fsBtn = page.getByRole('button', { name: /Toàn màn hình|Fullscreen/i })
  const appeared = await fsBtn
    .first()
    .waitFor({ state: 'visible', timeout: 5_000 })
    .then(() => true)
    .catch(() => false)
  if (!appeared) return

  for (let attempt = 0; attempt < 3; attempt++) {
    await fsBtn.first().click().catch(() => {})
    const stillThere = await fsBtn
      .first()
      .isVisible()
      .catch(() => false)
    if (!stillThere) return
    await page.waitForTimeout(500)
  }
}
