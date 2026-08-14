const assert = require('assert').strict;
const { chromium } = require('playwright');

const baseUrl = process.env.UX_BASE_URL || 'https://localhost:44300';
const viewports = [
    { name: 'desktop', width: 1440, height: 900 },
    { name: 'mobile', width: 390, height: 844 }
];
const routes = [
    '/',
    '/catalog',
    '/product/ktm-ka-tombo-moco',
    '/cart',
    '/checkout',
    '/terms',
    '/privacy',
    '/search?q=KTM'
];

async function assertNoOverflow(page) {
    return page.evaluate(() => document.documentElement.scrollWidth <= innerWidth);
}

async function runViewport(browser, viewport) {
    const context = await browser.newContext({
        ignoreHTTPSErrors: true,
        viewport: { width: viewport.width, height: viewport.height }
    });
    const page = await context.newPage();
    const consoleErrors = [];
    const requestFailures = [];

    page.on('console', message => {
        if (message.type() === 'error') consoleErrors.push(message.text());
    });
    page.on('pageerror', error => consoleErrors.push(error.message));
    page.on('requestfailed', request => requestFailures.push({
        url: request.url(),
        error: request.failure()?.errorText
    }));

    const routeResults = [];
    await page.goto(`${baseUrl}/cart`, { waitUntil: 'networkidle' });
    while (await page.locator('button[aria-label^="Remover "]').count()) {
        await page.locator('button[aria-label^="Remover "]').first().click();
        await page.waitForLoadState('networkidle');
    }

    for (const route of routes) {
        const response = await page.goto(`${baseUrl}${route}`, { waitUntil: 'networkidle' });
        assert.equal(response?.status(), 200, `${viewport.name}: ${route} returned ${response?.status()}`);
        routeResults.push({
            route,
            status: response?.status(),
            noOverflow: await assertNoOverflow(page),
            title: await page.title()
        });
    }

    await page.goto(`${baseUrl}/`, { waitUntil: 'networkidle' });
    const images = await page.locator('img').evaluateAll(elements => elements.map(image => ({
        alt: image.getAttribute('alt'),
        loaded: image.complete && image.naturalWidth > 0
    })));
    assert.ok(images.every(image => image.loaded), `${viewport.name}: broken homepage image`);

    if (viewport.name === 'mobile') {
        await page.getByRole('button', { name: 'Abrir menu' }).click();
        assert.equal(await page.locator('body').evaluate(body => body.classList.contains('menu-open')), true);
        assert.equal(await page.locator('.mobile-menu-overlay').isVisible(), true);
        assert.equal(await page.locator('#primary-navigation').evaluate(nav => nav.classList.contains('is-open')), true);
        await page.keyboard.press('Escape');
        assert.equal(await page.locator('.mobile-menu-overlay').evaluate(overlay => overlay.classList.contains('is-open')), false);
    }

    await page.goto(`${baseUrl}/product/ktm-ka-tombo-moco`, { waitUntil: 'networkidle' });
    await page.locator('select').selectOption({ index: 1 });
    await page.getByRole('button', { name: /Adicionar ao Carrinho/ }).click();
    await page.waitForLoadState('networkidle');
    assert.match(page.url(), /\/cart$/);

    const quantity = page.locator('input[type="number"]').first();
    await quantity.fill('2');
    await quantity.blur();
    await page.waitForLoadState('networkidle');
    const cartText = await page.locator('body').innerText();
    assert.match(cartText, /Subtotal:\s*39,80 €/);
    assert.match(cartText, /Total:\s*44,79 €/);

    await page.goto(`${baseUrl}/checkout`, { waitUntil: 'networkidle' });
    await page.getByRole('button', { name: /Continuar para pagamento/ }).click();
    const invalidCheckout = await page.locator('body').innerText();
    assert.match(invalidCheckout, /Indica o teu nome completo/);
    assert.match(invalidCheckout, /Aceita os termos e condições/);

    await page.getByRole('textbox', { name: /Nome completo/ }).fill('Cliente UX');
    await page.getByRole('textbox', { name: /^Email$/ }).fill('cliente.ux@example.com');
    await page.getByRole('textbox', { name: /Telemóvel/ }).fill('912345678');
    await page.getByRole('textbox', { name: /^Morada$/ }).fill('Rua de Teste, 1');
    await page.getByRole('textbox', { name: /Código postal/ }).fill('1000-001');
    await page.getByRole('textbox', { name: /Localidade/ }).fill('Lisboa');
    await page.getByRole('checkbox', { name: /Li e aceito/ }).check();
    assert.equal(await page.getByRole('button', { name: /Continuar para pagamento/ }).isEnabled(), true);

    const result = {
        viewport: viewport.name,
        routes: routeResults,
        images,
        consoleErrors,
        requestFailures,
        paymentSubmitted: false
    };
    await context.close();
    return result;
}

(async () => {
    const browser = await chromium.launch({
        headless: true,
        executablePath: process.env.PLAYWRIGHT_EXECUTABLE_PATH || undefined
    });
    try {
        const results = [];
        for (const viewport of viewports) results.push(await runViewport(browser, viewport));
        console.log(JSON.stringify({ baseUrl, results }, null, 2));
    } finally {
        await browser.close();
    }
})().catch(error => {
    console.error(error.stack || error);
    process.exitCode = 1;
});