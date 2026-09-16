import http from 'node:http';
import { readFile, stat } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const MODULE_ROOT = path.join(HERE, 'node_modules');
const PORT = Number(process.env.PORT || 4173);
const HOST = process.env.HOST || '127.0.0.1';

const MIME = new Map([
  ['.html', 'text/html; charset=utf-8'],
  ['.js', 'text/javascript; charset=utf-8'],
  ['.mjs', 'text/javascript; charset=utf-8'],
  ['.css', 'text/css; charset=utf-8'],
  ['.json', 'application/json; charset=utf-8'],
  ['.svg', 'image/svg+xml'],
  ['.png', 'image/png'],
]);

function within(root, candidate) {
  return candidate === root || candidate.startsWith(root + path.sep);
}

function resolveRequest(urlPath) {
  const pathname = decodeURIComponent(urlPath.split('?')[0]);
  if (pathname === '/' || pathname === '') return path.join(HERE, 'index.html');
  const relative = pathname.replace(/^\/+/, '');
  if (relative.startsWith('vendor/')) {
    const candidate = path.resolve(MODULE_ROOT, relative.slice('vendor/'.length));
    return within(MODULE_ROOT, candidate) ? candidate : null;
  }
  const candidate = path.resolve(HERE, relative);
  return within(HERE, candidate) ? candidate : null;
}

async function handler(request, response) {
  if (request.url === '/health') {
    response.writeHead(200, { 'content-type': 'application/json; charset=utf-8' });
    response.end(JSON.stringify({ ok: true, prototype: 'musca-gate-3d', version: '0.2.0' }));
    return;
  }
  const filePath = resolveRequest(request.url || '/');
  if (!filePath) {
    response.writeHead(403);
    response.end('forbidden');
    return;
  }
  try {
    const info = await stat(filePath);
    if (!info.isFile()) throw new Error('not file');
    const bytes = await readFile(filePath);
    response.writeHead(200, {
      'content-type': MIME.get(path.extname(filePath)) || 'application/octet-stream',
      'cache-control': 'no-store',
    });
    response.end(bytes);
  } catch {
    response.writeHead(404, { 'content-type': 'text/plain; charset=utf-8' });
    response.end('not found');
  }
}

if (process.argv.includes('--check')) {
  const threePath = resolveRequest('/vendor/three/build/three.module.js');
  const controlsPath = resolveRequest('/vendor/three/examples/jsm/controls/PointerLockControls.js');
  const [threeInfo, controlsInfo] = await Promise.all([stat(threePath), stat(controlsPath)]);
  console.log(JSON.stringify({
    status: 'PASS',
    host: HOST,
    port: PORT,
    threeBytes: threeInfo.size,
    pointerLockBytes: controlsInfo.size,
  }));
  process.exit(0);
}

const server = http.createServer(handler);
server.listen(PORT, HOST, () => {
  console.log(`MUSCA 3D prototype: http://${HOST}:${PORT}`);
});

for (const signal of ['SIGINT', 'SIGTERM']) {
  process.on(signal, () => server.close(() => process.exit(0)));
}
