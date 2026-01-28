import { test, expect } from '@playwright/test';

test('create and delete todo', async ({ page }) => {
  const todoText = `ci-smoke-${Date.now()}`;

  await page.goto('/');

  const input = page.getByRole('textbox', { name: 'New todo title' });
  await input.fill(todoText);

  await page.getByRole('button', { name: 'Add' }).click();

  const todoItem = page.getByRole('listitem').filter({ hasText: todoText });
  await expect(todoItem).toBeVisible();

  await todoItem.getByRole('button', { name: 'Delete todo' }).click();

  await expect(todoItem).toHaveCount(0);
});
