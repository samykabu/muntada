import { test, expect } from '@playwright/test';

test.describe('Login Flow', () => {
  test('login page loads with form', async ({ page }) => {
    await page.goto('/login');
    await expect(page.locator('h1')).toContainText('Sign In');
    await expect(page.locator('input#login-email')).toBeVisible();
    await expect(page.locator('input#login-password')).toBeVisible();
  });

  test('login page has forgot password link', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByText('Forgot password?')).toBeVisible();
  });

  test('login page links to registration', async ({ page }) => {
    await page.goto('/login');
    await expect(page.locator('a[href="/register"]')).toBeVisible();
  });

  test('login page links to OTP login', async ({ page }) => {
    await page.goto('/login');
    await expect(page.locator('a[href="/login/otp"]')).toBeVisible();
  });
});
