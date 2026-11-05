import test from 'node:test';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { handle } from '../plugins/codex-screenshot/server.mjs';

const call = args => ({ jsonrpc: '2.0', id: 3, method: 'tools/call', params: { name: 'capture_desktop', arguments: args } });
const png = 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aX1sAAAAASUVORK5CYII=';

test('initialize, tool discovery, and notifications work over stdio', () => {
  const input = [
    { jsonrpc: '2.0', id: 1, method: 'initialize', params: { protocolVersion: '2025-06-18' } },
    { jsonrpc: '2.0', method: 'notifications/initialized' },
    { jsonrpc: '2.0', id: 2, method: 'tools/list' }
  ].map(JSON.stringify).join('\n') + '\n';
  const proc = spawnSync(process.execPath, ['plugins/codex-screenshot/server.mjs'], { input, encoding: 'utf8', timeout: 5000 });
  assert.equal(proc.status, 0, proc.stderr);
  const responses = proc.stdout.trim().split('\n').map(JSON.parse);
  assert.equal(responses.length, 2);
  assert.equal(responses[0].result.protocolVersion, '2025-06-18');
  assert.equal(responses[0].result.serverInfo.name, 'codex-screenshot');
  assert.equal(responses[1].result.tools[0].name, 'capture_desktop');
});

test('capture produces an MCP image and forwards validated options', async () => {
  const response = await handle(call({ target: 'all', delay_seconds: 2 }), async (target, delay) => {
    assert.equal(target, 'all');
    assert.equal(delay, 2);
    return { width: 1, height: 1, data: png };
  });
  assert.deepEqual(response.result.content[1], { type: 'image', mimeType: 'image/png', data: png });
});

test('invalid options never invoke screen capture', async () => {
  for (const args of [{ target: 'x' }, { delay_seconds: 11 }, { delay_seconds: 0.5 }, { command: 'anything' }, []]) {
    const response = await handle(call(args), async () => assert.fail('Capture must not run'));
    assert.equal(response.result.isError, true);
  }
});

test('capture errors are returned without breaking the MCP server', async () => {
  const response = await handle(call({}), async () => { throw new Error('desktop locked'); });
  assert.equal(response.result.isError, true);
  assert.match(response.result.content[0].text, /desktop locked/);
  assert.equal((await handle({ jsonrpc: '2.0', id: 4, method: 'ping' })).id, 4);
});
