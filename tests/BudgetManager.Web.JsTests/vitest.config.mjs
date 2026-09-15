import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { defineConfig } from 'vitest/config';

const projectDirectory = path.dirname(fileURLToPath(import.meta.url));

const exposeSiteUtilitiesForTests = {
    name: 'expose-site-utilities-for-tests',
    enforce: 'post',
    transform(code, id) {
        const normalizedId = id.replaceAll('\\', '/').split('?')[0];

        if (!normalizedId.endsWith('/src/BudgetManager.Web/wwwroot/js/site.js'))
            return null;

        return {
            code: `${code}\nexport { textToHtml, validateForm, applyMultiSelectWidth };\n`,
            map: null
        };
    }
};

const productionJavaScript = path
    .resolve(projectDirectory, '../../src/BudgetManager.Web/wwwroot/js/**/*.js')
    .replaceAll('\\', '/');

export default defineConfig({
    plugins: [exposeSiteUtilitiesForTests],
    test: {
        include: ['tests/**/*.test.js'],
        environment: 'jsdom',
        clearMocks: true,
        restoreMocks: true,
        coverage: {
            provider: 'v8',
            allowExternal: true,
            include: [productionJavaScript],
            reporter: ['text', 'html'],
            reportsDirectory: 'coverage',
            thresholds: {
                statements: 0.01,
                branches: 0.01,
                functions: 0.01,
                lines: 0.01
            }
        }
    }
});
