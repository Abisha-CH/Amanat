const { test, expect } = require('@playwright/test');

const baseURL = process.env.AMANAT_BASE_URL || 'http://127.0.0.1:4173';
const publicPassword = 'Amanat@2026';
const seededUsers = {
  Organizer: {
    email: process.env.AMANAT_SEED_ORGANIZER_EMAIL || 'organizer.seed@amanat.test',
    password: process.env.AMANAT_SEED_ORGANIZER_PASSWORD || 'SeedOrganizer@2026'
  },
  Treasurer: {
    email: process.env.AMANAT_SEED_TREASURER_EMAIL || 'treasurer.seed@amanat.test',
    password: process.env.AMANAT_SEED_TREASURER_PASSWORD || 'SeedTreasurer@2026'
  }
};

function unique(prefix) {
  return prefix + '-' + Date.now() + '-' + Math.random().toString(36).slice(2, 7);
}

async function stubExternal(page) {
  await page.route('**/*', route => {
    const url = route.request().url();
    if (url.startsWith(baseURL)) return route.continue();
    if (url.includes('/bootstrap@5.3.3/dist/css/bootstrap.min.css')) {
      return route.fulfill({ status: 200, contentType: 'text/css', body: '' });
    }
    if (url.includes('/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js')) {
      return route.fulfill({ status: 200, contentType: 'application/javascript', body: '' });
    }
    if (url.includes('bootstrap-icons') || url.includes('fonts.googleapis.com') || url.includes('fonts.gstatic.com')) {
      return route.fulfill({ status: 200, contentType: 'text/css', body: '' });
    }
    return route.abort();
  });
}

async function registerContributor(page, email) {
  await page.goto('/Identity/Account/Register');
  await page.fill('#fullName', 'Ajax Contributor');
  await page.fill('#email', email);
  await page.fill('#phone', '03001234567');
  await page.fill('#password', publicPassword);
  await page.fill('#confirm-password', publicPassword);
  await page.check('input[name="Input.Role"][value="Contributor"]');
  await page.check('#terms');
  await page.click('button[type=submit]');
  await expect(page).toHaveURL(/\/Contributor\/Dashboard$/);
}

async function login(page, role) {
  await page.goto('/Identity/Account/Login');
  await page.fill('#email', seededUsers[role].email);
  await page.fill('#password', seededUsers[role].password);
  await page.click('button[type=submit]');
  await expect(page).toHaveURL(new RegExp(`/${role}/Dashboard$`));
}

async function submitReference(page, reference, amount) {
  const form = page.locator('form[data-ajax-submit="true"]');
  const responses = [];
  const capture = response => {
    if (response.url().includes('/Contributor/RecordReferenceJson') && response.request().method() === 'POST') {
      responses.push(response);
    }
  };
  page.on('response', capture);
  await form.locator('#campaign-select').selectOption('education-for-all');
  await form.locator('#amount-input').fill(String(amount));
  await form.locator('#trx-id').fill(reference);
  await form.locator('button[type=submit]').click();
  await expect(page.locator('#history-section tbody tr').first()).toContainText(reference);
  await page.waitForTimeout(150);
  page.off('response', capture);
  const response = responses[responses.length - 1];
  expect(response).toBeDefined();
  expect(response.status()).toBe(200);
  return response.json();
}

async function postJson(page, url, payload, includeToken) {
  return page.evaluate(async options => {
    const headers = { 'Content-Type': 'application/json' };
    if (options.includeToken) {
      const token = document.querySelector('input[name="__RequestVerificationToken"]');
      if (token) headers.RequestVerificationToken = token.value;
    }
    const response = await fetch(options.url, {
      method: 'POST',
      headers,
      body: JSON.stringify(options.payload),
      credentials: 'same-origin'
    });
    let body = null;
    try {
      body = await response.json();
    } catch (error) {
      body = null;
    }
    return { status: response.status, body };
  }, { url, payload, includeToken });
}

test('jQuery loads locally and $.get refreshes campaign summary', async ({ page }) => {
  await stubExternal(page);
  const summaryPromise = page.waitForResponse(response => response.url().includes('/Campaigns/Summary/'));
  await page.goto('/Campaigns/Details/education-for-all');
  await page.waitForFunction(() => Boolean(window.jQuery));
  const summaryResponse = await summaryPromise;
  expect(summaryResponse.status()).toBe(200);

  const summary = await summaryResponse.json();
  const details = await page.evaluate(() => {
    const scripts = [...document.scripts].filter(script => script.src.includes('jquery-3.7.1.min.js'));
    return {
      count: scripts.length,
      local: scripts.every(script => new URL(script.src).origin === window.location.origin),
      version: window.jQuery.fn.jquery,
      raised: document.querySelector('#summary-raised').textContent.trim(),
      goal: document.querySelector('#summary-goal').textContent.trim(),
      balance: document.querySelector('#summary-balance').textContent.trim(),
      funded: document.querySelector('#summary-funded').textContent.trim(),
      width: document.querySelector('#summary-progress-bar').style.width,
      status: document.querySelector('#campaign-status-text').textContent.trim()
    };
  });

  expect(details.count).toBe(1);
  expect(details.local).toBe(true);
  expect(details.version).toBe('3.7.1');
  expect(details.raised).toBe(summary.data.raisedText);
  expect(details.goal).toBe(summary.data.goalText);
  expect(details.balance).toBe(summary.data.balanceText);
  expect(details.funded).toBe(summary.data.progressPercent + '% Funded');
  expect(details.width).toBe(summary.data.progressPercent + '%');
  expect(details.status).toBe(summary.data.status + ' Campaign');
});

test('$.post submits contributor references and returns structured validation errors', async ({ page }) => {
  await stubExternal(page);
  await registerContributor(page, unique('ajax-contributor') + '@amanat.test');

  const form = page.locator('form[data-ajax-submit="true"]');
  await form.locator('#amount-input').fill('');
  await form.locator('#trx-id').fill('AJAX-INVALID');
  await form.locator('button[type=submit]').click();
  await expect(form).toHaveClass(/was-validated/);
  await expect(form.locator('#amount-input')).toHaveJSProperty('validity.valid', false);
  expect(page.url()).toContain('/Contributor/Dashboard');

  const reference = unique('AJAX-TRX');
  const payload = {
    Campaign: 'education-for-all',
    Amount: '7777',
    TransactionId: reference,
    DateSent: '2023-10-24',
    Notes: 'AJAX test'
  };
  const body = await submitReference(page, reference, 7777);
  expect(body.success).toBe(true);
  expect(body.message).toContain(reference);
  await expect(page.locator('#amanat-toast')).toContainText(reference);
  await expect(page.locator('#reference-ajax-alert')).toBeVisible();
  await expect(page.locator('#history-section tbody tr').first()).toContainText('Under Review');

  const duplicate = await postJson(page, '/Contributor/RecordReferenceJson', payload, true);
  expect(duplicate.status).toBe(400);
  expect(JSON.stringify(duplicate.body.errors)).toContain('already been submitted');
  await expect(page.locator('#history-section tbody tr')).toHaveCount(1);
  expect(page.url()).toContain('/Contributor/Dashboard');
});

test('JSON endpoints reject invalid antiforgery, anonymous, and wrong-role callers', async ({ browser }) => {
  const payload = {
    Campaign: 'education-for-all',
    Amount: '100',
    TransactionId: unique('AJAX-BLOCKED'),
    DateSent: '2023-10-24'
  };
  const decisionPayload = {
    ContributionId: 1,
    RowVersion: Buffer.from('stale123').toString('base64'),
    Decision: 'Verified'
  };

  const anonymousContext = await browser.newContext({ baseURL });
  const anonymous = await anonymousContext.newPage();
  await stubExternal(anonymous);
  await anonymous.goto('/Campaigns/Details/education-for-all');
  const noToken = await postJson(anonymous, '/Contributor/RecordReferenceJson', payload, false);
  expect(noToken.status).toBe(400);
  const anonymousPost = await postJson(anonymous, '/Contributor/RecordReferenceJson', payload, true);
  expect(anonymousPost.status).toBe(401);
  const anonymousDecision = await postJson(anonymous, '/Treasurer/DecideContributionJson', decisionPayload, true);
  expect(anonymousDecision.status).toBe(401);
  await anonymousContext.close();

  const organizerContext = await browser.newContext({ baseURL });
  const organizer = await organizerContext.newPage();
  await stubExternal(organizer);
  await login(organizer, 'Organizer');
  const organizerContribution = await postJson(organizer, '/Contributor/RecordReferenceJson', payload, true);
  expect(organizerContribution.status).toBe(403);
  const organizerDecision = await postJson(organizer, '/Treasurer/DecideContributionJson', decisionPayload, true);
  expect(organizerDecision.status).toBe(403);
  await organizerContext.close();

  const treasurerContext = await browser.newContext({ baseURL });
  const treasurer = await treasurerContext.newPage();
  await stubExternal(treasurer);
  await login(treasurer, 'Treasurer');
  const treasurerExpense = await postJson(treasurer, '/Organizer/DecideExpenseJson', {
    ExpenseId: 1,
    RowVersion: Buffer.from('stale123').toString('base64'),
    Decision: 'Approved'
  }, true);
  expect(treasurerExpense.status).toBe(403);
  const noTokenDecision = await postJson(treasurer, '/Treasurer/DecideContributionJson', decisionPayload, false);
  expect(noTokenDecision.status).toBe(400);
  await treasurerContext.close();
});

test('$.ajax verifies and rejects contributions and returns stale conflicts', async ({ page, browser }) => {
  await stubExternal(page);
  await registerContributor(page, unique('ajax-decision-contributor') + '@amanat.test');
  const verifyRef = unique('AJAX-VERIFY');
  const rejectRef = unique('AJAX-REJECT');
  await submitReference(page, verifyRef, 5100);
  await submitReference(page, rejectRef, 5200);

  const context = await browser.newContext({ baseURL });
  const treasurer = await context.newPage();
  await stubExternal(treasurer);
  await login(treasurer, 'Treasurer');

  const verifyRow = treasurer.locator('#verification-table tbody tr').filter({ hasText: verifyRef });
  const verifyId = Number(await verifyRow.locator('input[name="ContributionId"]').first().inputValue());
  const stale = await postJson(treasurer, '/Treasurer/DecideContributionJson', {
    ContributionId: verifyId,
    RowVersion: Buffer.from('stale123').toString('base64'),
    Decision: 'Verified'
  }, true);
  expect(stale.status).toBe(409);

  const verifyPromise = treasurer.waitForResponse(response =>
    response.url().includes('/Treasurer/DecideContributionJson') && response.request().method() === 'POST');
  await verifyRow.locator('.js-verify-btn').click();
  const verifyResponse = await verifyPromise;
  expect(verifyResponse.status()).toBe(200);
  expect((await verifyResponse.json()).message).toContain('verified');
  await expect(verifyRow.locator('.js-status')).toContainText('Verified');
  await expect(treasurer.locator('#amanat-toast')).toContainText('verified');

  const rejectRow = treasurer.locator('#verification-table tbody tr').filter({ hasText: rejectRef });
  const rejectPromise = treasurer.waitForResponse(response =>
    response.url().includes('/Treasurer/DecideContributionJson') && response.request().method() === 'POST');
  await rejectRow.locator('.js-reject-btn').click();
  const rejectResponse = await rejectPromise;
  expect(rejectResponse.status()).toBe(200);
  await expect(rejectRow.locator('.js-status')).toContainText('Rejected');
  expect(treasurer.url()).toContain('/Treasurer/Dashboard');
  await context.close();
});

test('$.ajax approves and rejects expenses and returns stale conflicts', async ({ page, browser }) => {
  await stubExternal(page);
  await login(page, 'Treasurer');
  const approveVendor = unique('AJAX-APPROVE-VENDOR');
  const rejectVendor = unique('AJAX-REJECT-VENDOR');

  for (const vendor of [approveVendor, rejectVendor]) {
    await page.selectOption('#campaign-select', 'education-for-all');
    await page.fill('#expense-amount', '3100');
    await page.fill('#payee-name', vendor);
    await page.fill('#expense-desc', 'AJAX expense decision test');
    await page.click('form[action*="SubmitExpense"] button[type=submit]');
    await expect(page.locator('body')).toContainText('submitted for organizer approval');
  }

  const context = await browser.newContext({ baseURL });
  const organizer = await context.newPage();
  await stubExternal(organizer);
  await login(organizer, 'Organizer');

  const approveRow = organizer.locator('#expense-approvals-table tbody tr').filter({ hasText: approveVendor });
  const approveId = Number(await approveRow.locator('input[name="ExpenseId"]').first().inputValue());
  const stale = await postJson(organizer, '/Organizer/DecideExpenseJson', {
    ExpenseId: approveId,
    RowVersion: Buffer.from('stale123').toString('base64'),
    Decision: 'Approved'
  }, true);
  expect(stale.status).toBe(409);

  const approvePromise = organizer.waitForResponse(response =>
    response.url().includes('/Organizer/DecideExpenseJson') && response.request().method() === 'POST');
  await approveRow.locator('.js-approve-btn').click();
  const approveResponse = await approvePromise;
  expect(approveResponse.status()).toBe(200);
  await expect(approveRow.locator('.js-status')).toContainText('Approved');
  await expect(organizer.locator('#amanat-toast')).toContainText('approved');

  const rejectRow = organizer.locator('#expense-approvals-table tbody tr').filter({ hasText: rejectVendor });
  const rejectPromise = organizer.waitForResponse(response =>
    response.url().includes('/Organizer/DecideExpenseJson') && response.request().method() === 'POST');
  await rejectRow.locator('.js-reject-btn').click();
  const rejectResponse = await rejectPromise;
  expect(rejectResponse.status()).toBe(200);
  await expect(rejectRow.locator('.js-status')).toContainText('Rejected');
  expect(organizer.url()).toContain('/Organizer/Dashboard');
  await context.close();
});

test('normal MVC POST fallback works without JavaScript', async ({ browser }) => {
  test.setTimeout(60000);
  const contributorContext = await browser.newContext({ baseURL });
  const contributor = await contributorContext.newPage();
  await registerContributor(contributor, unique('fallback-contributor') + '@amanat.test');
  const contributorState = await contributorContext.storageState();
  await contributorContext.close();

  const reference = unique('FALLBACK-NOJS');
  const fallbackContext = await browser.newContext({ baseURL, javaScriptEnabled: false, storageState: contributorState });
  const fallback = await fallbackContext.newPage();
  await fallback.goto('/Contributor/Dashboard');
  await fallback.selectOption('#campaign-select', 'education-for-all');
  await fallback.fill('#amount-input', '9200');
  await fallback.fill('#trx-id', reference);
  await fallback.click('form[action*="RecordReference"] button[type=submit]');
  await expect(fallback.locator('body')).toContainText('submitted for treasurer verification');
  await fallbackContext.close();

  const treasurerContext = await browser.newContext({ baseURL });
  const treasurer = await treasurerContext.newPage();
  await login(treasurer, 'Treasurer');
  const treasurerState = await treasurerContext.storageState();
  await treasurerContext.close();

  const decisionContext = await browser.newContext({ baseURL, javaScriptEnabled: false, storageState: treasurerState });
  const decisionPage = await decisionContext.newPage();
  await decisionPage.goto('/Treasurer/Dashboard');
  const row = decisionPage.locator('#verification-table tbody tr').filter({ hasText: reference });
  await row.locator('.js-verify-btn').click();
  await expect(decisionPage.locator('body')).toContainText('Contribution verified and added to the public ledger.');
  await decisionContext.close();
});
