import { test, expect } from '@playwright/test';

test.describe('Smoke Tests', () => {
  test('frontend loads and displays Muntada heading', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('h1')).toContainText('Muntada');
  });

  test('page has correct title', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveTitle(/Muntada|Vite/);
  });
});
