import { createInterface } from 'node:readline';
import { execFile } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { promisify } from 'node:util';
import { resolve } from 'node:path';

const runFile = promisify(execFile);
const script = fileURLToPath(new URL('./capture.ps1', import.meta.url));
const versions = ['2025-11-25', '2025-06-18', '2025-03-26', '2024-11-05'];
const tool = {
  name: 'capture_desktop',
  description: 'Capture a PNG of the Windows desktop and return it as an image in this conversation. Use only when the user requests a screenshot. Default: primary monitor. Captures visible content, including any windows covering the desktop. No background monitoring or disk storage.',
  inputSchema: {
    type: 'object',
    properties: {
      target: { type: 'string', enum: ['primary', 'all'], default: 'primary', description: 'Primary monitor or the entire multi-monitor desktop.' },
      delay_seconds: { type: 'integer', minimum: 0, maximum: 10, default: 0, description: 'Seconds to switch to the desired window before capture.' }
    },
    additionalProperties: false
  },
  annotations: { title: 'Tirar print do desktop', readOnlyHint: true, destructiveHint: false, openWorldHint: false }
};

export function validateArguments(args) {
  if (!args || typeof args !== 'object' || Array.isArray(args)) throw new Error('Arguments must be an object.');
  if (Object.keys(args).some(key => !['target', 'delay_seconds'].includes(key))) throw new Error('Unknown argument.');
  const target = args.target ?? 'primary';
  const delay = args.delay_seconds ?? 0;
  if (!['primary', 'all'].includes(target)) throw new Error('target must be primary or all.');
  if (!Number.isInteger(delay) || delay < 0 || delay > 10) throw new Error('delay_seconds must be an integer from 0 to 10.');
  return { target, delay };
}

export async function handle(request, capture = captureDesktop) {
  if (!request || request.jsonrpc !== '2.0' || typeof request.method !== 'string') {
    return { jsonrpc: '2.0', id: request?.id ?? null, error: { code: -32600, message: 'Invalid Request' } };
  }
  if (request.id === undefined) return null;
  const reply = result => ({ jsonrpc: '2.0', id: request.id, result });
  if (request.method === 'initialize') return reply({
    protocolVersion: versions.includes(request.params?.protocolVersion) ? request.params.protocolVersion : versions[0],
    capabilities: { tools: {} },
    serverInfo: { name: 'codex-screenshot', version: '0.2.0' },
    instructions: 'Capture only when the user asks. Return the image in your reply if the host supports it. This tool supplies image context; it does not insert attachments into the message composer.'
  });
  if (request.method === 'ping') return reply({});
  if (request.method === 'tools/list') return reply({ tools: [tool] });
  if (request.method === 'tools/call') {
    if (request.params?.name !== tool.name) return { jsonrpc: '2.0', id: request.id, error: { code: -32602, message: 'Unknown tool' } };
    try {
      const { target, delay } = validateArguments(request.params.arguments ?? {});
      const shot = await capture(target, delay);
      if (!shot || !Number.isInteger(shot.width) || shot.width <= 0 || !Number.isInteger(shot.height) || shot.height <= 0 || typeof shot.data !== 'string' || !shot.data.startsWith('iVBORw0KGgo')) {
        throw new Error('Capture did not return a valid PNG.');
      }
      return reply({ content: [
        { type: 'text', text: `Print: ${shot.width} x ${shot.height} px (${target}).` },
        { type: 'image', mimeType: 'image/png', data: shot.data }
      ] });
    } catch (error) {
      return reply({ isError: true, content: [{ type: 'text', text: `Nao foi possivel capturar a tela: ${error.message}` }] });
    }
  }
  return { jsonrpc: '2.0', id: request.id, error: { code: -32601, message: 'Method not found' } };
}

async function captureDesktop(target, delay) {
  if (process.platform !== 'win32') throw new Error('Windows is required.');
  try {
    const { stdout } = await runFile('powershell.exe', [
      '-NoLogo', '-NoProfile', '-NonInteractive', '-STA', '-ExecutionPolicy', 'Bypass',
      '-File', script, '-Target', target, '-DelaySeconds', String(delay)
    ], { windowsHide: true, timeout: 30000, maxBuffer: 24 * 1024 * 1024 });
    return JSON.parse(stdout.replace(/^\uFEFF/, '').trim());
  } catch (error) {
    throw new Error(error.stderr?.trim() || error.message);
  }
}

if (process.argv[1] && fileURLToPath(import.meta.url) === resolve(process.argv[1])) {
  const input = createInterface({ input: process.stdin, crlfDelay: Infinity });
  for await (const line of input) {
    if (!line.trim()) continue;
    let response;
    try { response = await handle(JSON.parse(line)); }
    catch { response = { jsonrpc: '2.0', id: null, error: { code: -32700, message: 'Parse error' } }; }
    if (response) process.stdout.write(JSON.stringify(response) + '\n');
  }
}
