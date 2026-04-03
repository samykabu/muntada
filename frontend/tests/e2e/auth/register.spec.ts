import { test, expect } from '@playwright/test';

test.describe('Registration Flow', () => {
  test('register page loads with form', async ({ page }) => {
    await page.goto('/register');
    await expect(page.locator('h1')).toContainText('Create Account');
    await expect(page.locator('input#email')).toBeVisible();
    await expect(page.locator('input#password')).toBeVisible();
    await expect(page.locator('input#confirmPassword')).toBeVisible();
  });

  test('register form validates required fields', async ({ page }) => {
    await page.goto('/register');
    await page.click('button[type="submit"]');
    // HTML5 validation should prevent submission
    await expect(page.locator('input#email:invalid')).toBeVisible();
  });

  test('register page links to login', async ({ page }) => {
    await page.goto('/register');
    await expect(page.locator('a[href="/login"]')).toBeVisible();
  });
});
