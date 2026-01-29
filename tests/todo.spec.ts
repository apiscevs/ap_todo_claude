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

test('priority selector defaults to Medium', async ({ page }) => {
  await page.goto('/');

  const mediumRadio = page.getByRole('radio', { name: 'Medium' });
  await expect(mediumRadio).toBeChecked();
});

test('create todo with Low priority', async ({ page }) => {
  const todoText = `low-priority-${Date.now()}`;

  await page.goto('/');

  const input = page.getByRole('textbox', { name: 'New todo title' });
  await input.fill(todoText);

  await page.getByRole('radio', { name: 'Low' }).click();
  await page.getByRole('button', { name: 'Add' }).click();

  const todoItem = page.getByRole('listitem').filter({ hasText: todoText });
  await expect(todoItem).toBeVisible();
  await expect(todoItem.getByText('Low')).toBeVisible();

  // Clean up
  await todoItem.getByRole('button', { name: 'Delete todo' }).click();
});

test('create todo with Medium priority', async ({ page }) => {
  const todoText = `medium-priority-${Date.now()}`;

  await page.goto('/');

  const input = page.getByRole('textbox', { name: 'New todo title' });
  await input.fill(todoText);

  // Medium is default, no need to click
  await page.getByRole('button', { name: 'Add' }).click();

  const todoItem = page.getByRole('listitem').filter({ hasText: todoText });
  await expect(todoItem).toBeVisible();
  await expect(todoItem.getByText('Medium')).toBeVisible();

  // Clean up
  await todoItem.getByRole('button', { name: 'Delete todo' }).click();
});

test('create todo with High priority', async ({ page }) => {
  const todoText = `high-priority-${Date.now()}`;

  await page.goto('/');

  const input = page.getByRole('textbox', { name: 'New todo title' });
  await input.fill(todoText);

  await page.getByRole('radio', { name: 'High' }).click();
  await page.getByRole('button', { name: 'Add' }).click();

  const todoItem = page.getByRole('listitem').filter({ hasText: todoText });
  await expect(todoItem).toBeVisible();
  await expect(todoItem.getByText('High')).toBeVisible();

  // Clean up
  await todoItem.getByRole('button', { name: 'Delete todo' }).click();
});

test('priority selector resets to Medium after creating todo', async ({ page }) => {
  const todoText = `reset-test-${Date.now()}`;

  await page.goto('/');

  const input = page.getByRole('textbox', { name: 'New todo title' });
  await input.fill(todoText);

  // Select High priority
  await page.getByRole('radio', { name: 'High' }).click();
  await expect(page.getByRole('radio', { name: 'High' })).toBeChecked();

  // Add todo
  await page.getByRole('button', { name: 'Add' }).click();

  // Check that priority reset to Medium
  await expect(page.getByRole('radio', { name: 'Medium' })).toBeChecked();
  await expect(page.getByRole('radio', { name: 'High' })).not.toBeChecked();

  // Clean up
  const todoItem = page.getByRole('listitem').filter({ hasText: todoText });
  await todoItem.getByRole('button', { name: 'Delete todo' }).click();
});

test('priority badges have correct colors', async ({ page }) => {
  const lowTodo = `low-color-${Date.now()}`;
  const mediumTodo = `medium-color-${Date.now()}`;
  const highTodo = `high-color-${Date.now()}`;

  await page.goto('/');

  // Create Low priority todo
  await page.getByRole('textbox', { name: 'New todo title' }).fill(lowTodo);
  await page.getByRole('radio', { name: 'Low' }).click();
  await page.getByRole('button', { name: 'Add' }).click();

  // Create Medium priority todo
  await page.getByRole('textbox', { name: 'New todo title' }).fill(mediumTodo);
  await page.getByRole('radio', { name: 'Medium' }).click();
  await page.getByRole('button', { name: 'Add' }).click();

  // Create High priority todo
  await page.getByRole('textbox', { name: 'New todo title' }).fill(highTodo);
  await page.getByRole('radio', { name: 'High' }).click();
  await page.getByRole('button', { name: 'Add' }).click();

  // Verify badges exist and have correct data attributes
  const lowItem = page.getByRole('listitem').filter({ hasText: lowTodo });
  const lowBadge = lowItem.locator('.priority-badge[data-priority="low"]');
  await expect(lowBadge).toBeVisible();
  await expect(lowBadge).toHaveText('Low');

  const mediumItem = page.getByRole('listitem').filter({ hasText: mediumTodo });
  const mediumBadge = mediumItem.locator('.priority-badge[data-priority="medium"]');
  await expect(mediumBadge).toBeVisible();
  await expect(mediumBadge).toHaveText('Medium');

  const highItem = page.getByRole('listitem').filter({ hasText: highTodo });
  const highBadge = highItem.locator('.priority-badge[data-priority="high"]');
  await expect(highBadge).toBeVisible();
  await expect(highBadge).toHaveText('High');

  // Clean up
  await lowItem.getByRole('button', { name: 'Delete todo' }).click();
  await mediumItem.getByRole('button', { name: 'Delete todo' }).click();
  await highItem.getByRole('button', { name: 'Delete todo' }).click();
});
