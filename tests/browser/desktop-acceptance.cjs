const fs = require('fs');
const path = require('path');
const { chromium } = require('playwright');

const origin = process.argv[2];
const evidence = process.argv[3];
if (!origin || !evidence) throw new Error('Usage: desktop-acceptance.cjs <origin> <evidence-dir>');
fs.mkdirSync(evidence, { recursive: true });

(async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  const requests = [];
  const errors = [];
  const context = await browser.newContext({ viewport: { width: 420, height: 620 }, javaScriptEnabled: true });
  await context.route('**/*', route => {
    const host = new URL(route.request().url()).hostname;
    return ['127.0.0.1', 'localhost', '::1'].includes(host) ? route.continue() : route.abort('blockedbyclient');
  });
  const page = await context.newPage();
  page.on('request', request => requests.push(request.url()));
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('pageerror', error => errors.push(error.message));

  await page.goto(`${origin}/Home/Compact`, { waitUntil: 'networkidle' });
  await page.screenshot({ path: path.join(evidence, 'compact-today-free-day.png'), fullPage: true });
  if (!(await page.getByText('Heute ist frei').isVisible())) throw new Error('Free-day state is missing');

  const names = [
    'Wasser trinken',
    'Zehn Minuten bewegen',
    'Eine sehr lange Gewohnheit mit einem außergewöhnlich ausführlichen Namen, der trotzdem vollständig lesbar bleiben muss',
    'Tageslicht', 'Lesen', 'Dehnen', 'Tagebuch', 'Medikamente'
  ];
  for (const name of names) {
    await page.goto(`${origin}/Habits/Create`);
    await page.getByLabel('Name').fill(name);
    await page.getByLabel('Startdatum').fill(new Date().toISOString().slice(0, 10));
    await page.getByRole('button', { name: /Habit anlegen/i }).click();
    await page.waitForLoadState('networkidle');
  }

  await page.goto(`${origin}/Home/Compact`, { waitUntil: 'networkidle' });
  await page.screenshot({ path: path.join(evidence, 'compact-today-desktop.png'), fullPage: true });
  const done = page.getByRole('button', { name: 'Wasser trinken erledigt' });
  await done.focus();
  await page.keyboard.press('Enter');
  await page.waitForFunction(() => document.querySelector('button[aria-label="Wasser trinken erledigt"]')?.getAttribute('aria-pressed') === 'true');
  if ((await page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth))) throw new Error('Compact desktop has horizontal overflow');

  await page.setViewportSize({ width: 320, height: 480 });
  await page.goto(`${origin}/Home/Compact`, { waitUntil: 'networkidle' });
  await page.screenshot({ path: path.join(evidence, 'compact-today-mobile-width.png'), fullPage: true });
  if ((await page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth))) throw new Error('Compact narrow view has horizontal overflow');

  await page.emulateMedia({ forcedColors: 'active' });
  await page.goto(`${origin}/Home/Compact`);
  if (!(await page.locator('.compact-action[aria-pressed="true"]').first().isVisible())) throw new Error('Selected outcome is not visible in forced-colors mode');
  await page.emulateMedia({ forcedColors: 'none' });

  await page.setViewportSize({ width: 1100, height: 780 });
  await page.goto(`${origin}/Home/Today`, { waitUntil: 'networkidle' });
  const assertCurrentNavigation = async expected => {
    const current = page.locator('.primary-nav a[aria-current="page"]');
    if (await current.count() !== 1 || (await current.textContent()).trim() !== expected) {
      throw new Error(`Expected ${expected} to be the only current navigation item`);
    }
  };
  await assertCurrentNavigation('Heute');
  const compactSwitch = page.getByRole('link', { name: 'Miniaturansicht öffnen' });
  await compactSwitch.focus();
  if (!(await compactSwitch.evaluate(element => element === document.activeElement))) throw new Error('Compact switch is not keyboard focusable');
  if (await compactSwitch.getAttribute('href') !== '/Home/Compact') throw new Error('Compact switch does not retain browser fallback navigation');
  const waterRow = page.locator('.habit-row').filter({ hasText: 'Wasser trinken' });
  const activeMarker = waterRow.getByRole('button', { name: 'Wasser trinken wieder als offen markieren' });
  await activeMarker.focus();
  await Promise.all([page.waitForNavigation(), page.keyboard.press('Enter')]);
  const pendingMarker = page.getByRole('button', { name: 'Wasser trinken erledigen' });
  if (await pendingMarker.getAttribute('aria-pressed') !== 'false') throw new Error('Status marker did not toggle Done to Pending');

  let doneAction = page.locator('.habit-row').filter({ hasText: 'Wasser trinken' }).getByRole('button', { name: 'Erledigt' });
  await Promise.all([page.waitForNavigation(), doneAction.click()]);
  doneAction = page.locator('.habit-row').filter({ hasText: 'Wasser trinken' }).getByRole('button', { name: 'Erledigt' });
  if (await doneAction.getAttribute('aria-pressed') !== 'true') throw new Error('Done action did not activate');
  await Promise.all([page.waitForNavigation(), doneAction.click()]);
  if (await page.locator('.habit-row').filter({ hasText: 'Wasser trinken' }).getByRole('button', { name: 'Erledigt' }).getAttribute('aria-pressed') !== 'false') throw new Error('Active Done action did not toggle to Pending');

  await page.screenshot({ path: path.join(evidence, 'desktop-full-today.png'), fullPage: true });
  await Promise.all([page.waitForNavigation(), page.locator('.habit-row .habit-copy h2 a').first().click()]);
  const backToToday = page.getByRole('link', { name: 'Zurück zu Heute' });
  await backToToday.focus();
  if (!(await backToToday.evaluate(element => element === document.activeElement))) throw new Error('Back to Today is not keyboard focusable');
  await page.screenshot({ path: path.join(evidence, 'habit-history-desktop.png'), fullPage: true });
  await page.setViewportSize({ width: 320, height: 640 });
  if (await page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth)) throw new Error('Narrow history has horizontal overflow');
  await page.screenshot({ path: path.join(evidence, 'habit-history-narrow.png'), fullPage: true });
  await Promise.all([page.waitForNavigation(), backToToday.click()]);

  await page.goto(`${origin}/Habits`, { waitUntil: 'networkidle' });
  await assertCurrentNavigation('Habits');
  await page.goto(`${origin}/Habits/Create`, { waitUntil: 'networkidle' });
  await assertCurrentNavigation('Habits');
  await page.goto(`${origin}/Backup`, { waitUntil: 'networkidle' });
  await assertCurrentNavigation('Daten');
  await page.emulateMedia({ forcedColors: 'active' });
  if (!(await page.locator('.primary-nav a[aria-current="page"]').isVisible())) throw new Error('Current navigation is not visible in forced-colors mode');
  await page.setViewportSize({ width: 320, height: 640 });
  if (await page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth)) throw new Error('Narrow full navigation has horizontal overflow');
  const surfaceSwitchOverlapsControl = await page.evaluate(() => {
    const switcher = document.querySelector('.surface-switch');
    if (!switcher) return true;
    const left = switcher.getBoundingClientRect();
    return [...document.querySelectorAll('a, button, input, select, textarea')]
      .filter(element => !switcher.contains(element))
      .some(element => {
        const right = element.getBoundingClientRect();
        return left.left < right.right && left.right > right.left && left.top < right.bottom && left.bottom > right.top;
      });
  });
  if (surfaceSwitchOverlapsControl) throw new Error('Compact switch overlaps another control at narrow width');
  await page.screenshot({ path: path.join(evidence, 'desktop-navigation-narrow.png'), fullPage: true });
  await page.emulateMedia({ forcedColors: 'none' });

  const noJsContext = await browser.newContext({ viewport: { width: 420, height: 620 }, javaScriptEnabled: false });
  await noJsContext.route('**/*', route => {
    const host = new URL(route.request().url()).hostname;
    return ['127.0.0.1', 'localhost', '::1'].includes(host) ? route.continue() : route.abort('blockedbyclient');
  });
  const noJsPage = await noJsContext.newPage();
  noJsPage.on('request', request => requests.push(request.url()));
  await noJsPage.goto(`${origin}/Home/Today`);
  const noJsCompactSwitch = noJsPage.getByRole('link', { name: 'Miniaturansicht öffnen' });
  await Promise.all([noJsPage.waitForNavigation(), noJsCompactSwitch.click()]);
  if (!noJsPage.url().endsWith('/Home/Compact')) throw new Error('No-JavaScript compact fallback navigation failed');
  const noJsFullSwitch = noJsPage.getByRole('link', { name: 'Ganze App öffnen' });
  await Promise.all([noJsPage.waitForNavigation(), noJsFullSwitch.click()]);
  if (!noJsPage.url().endsWith('/Home/Today')) throw new Error('No-JavaScript full fallback navigation failed');
  const noJsMarker = noJsPage.getByRole('button', { name: 'Wasser trinken erledigen' });
  await Promise.all([noJsPage.waitForNavigation(), noJsMarker.click()]);
  if (await noJsPage.getByRole('button', { name: 'Wasser trinken wieder als offen markieren' }).getAttribute('aria-pressed') !== 'true') throw new Error('No-JavaScript marker form did not persist');
  await noJsContext.close();

  await page.setViewportSize({ width: 1100, height: 780 });
  await page.goto(`${origin}/Home/Error`, { waitUntil: 'networkidle' });
  await page.screenshot({ path: path.join(evidence, 'compact-error-state.png'), fullPage: true });

  const nonLoopback = requests.filter(value => {
    const url = new URL(value);
    return !['127.0.0.1', 'localhost', '::1'].includes(url.hostname);
  });
  const report = { origin, recordedAtUtc: new Date().toISOString(), requests, nonLoopback, consoleErrors: errors };
  fs.writeFileSync(path.join(evidence, 'desktop-offline-network-log.json'), JSON.stringify(report, null, 2));
  if (nonLoopback.length) throw new Error(`External requests found: ${nonLoopback.join(', ')}`);
  if (errors.length) throw new Error(`Browser errors found: ${errors.join(', ')}`);
  await browser.close();
})().catch(error => { console.error(error); process.exit(1); });
