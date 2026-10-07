import { test } from 'node:test';
import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { mkdir, readFile } from 'node:fs/promises';
import path from 'node:path';

const root = path.resolve('.');
const dataDir = path.join(root, '.runtime', 'integration-data');
const executable = path.join(root, 'dist', 'portable', 'NXTROUTE.exe');
const headers = { Origin: 'http://127.0.0.1:3000', 'Content-Type': 'application/json' };
async function api(route, body) {
  const response = await fetch('http://127.0.0.1:3000' + route, body === undefined ? {} : { method: 'POST', headers, body: JSON.stringify(body) });
  assert.equal(response.status, 200, route);
  const text = await response.text();
  return text ? JSON.parse(text) : null;
}
async function waitFor(predicate, timeout = 25000) {
  const until = Date.now() + timeout;
  while (Date.now() < until) {
    try { const result = await predicate(); if (result) return result; } catch (error) { if (error instanceof assert.AssertionError) throw error; }
    await new Promise(resolve => setTimeout(resolve, 300));
  }
  throw new Error('Timed out waiting for gateway state');
}
test('Windows gateway: persistence, channel isolation, recovery, browser playback and process cleanup', { timeout: 160000 }, async () => {
  await mkdir(dataDir, { recursive: true });
  try { await fetch('http://127.0.0.1:3000', { signal: AbortSignal.timeout(500) }); throw new Error('Stop existing gateway before testing'); }
  catch (error) { if (error.message === 'Stop existing gateway before testing') throw error; }
  let gateway;
  const launch = () => {
    gateway = spawn(executable, ['--data-dir', dataDir, '--no-browser', 'true'], { windowsHide: true, stdio: ['pipe', 'pipe', 'pipe'], env: { ...process.env, DOTNET_BUNDLE_EXTRACT_BASE_DIR: path.join(root, '.runtime', 'bundle') } });
    gateway.stdout.on('data', () => {});
    gateway.stderr.on('data', () => {});
  };
  const terminate = () => new Promise(resolve => { gateway.once('exit', resolve); gateway.kill(); });
  let childPids = [];
  try {
    launch();
    await waitFor(async () => (await api('/api/channels')).router === 'online');
    await api('/api/channels/demo/start', {});
    await waitFor(async () => (await api('/api/channels')).channels.find(channel => channel.id === 'demo')?.ready);
    const denied = await fetch('http://127.0.0.1:3000/api/channels/demo/stop', { method: 'POST', headers: { ...headers, Origin: 'https://example.com' }, body: '{}' });
    assert.equal(denied.status, 403);
    const existing = (await api('/api/channels')).channels.find(channel => channel.name === 'Isolation test');
    const added = existing ?? await api('/api/channels', { name: 'Isolation test' });
    await api(`/api/channels/${added.id}/start`, {});
    await waitFor(async () => (await api('/api/channels')).channels.find(channel => channel.id === added.id)?.ready);
    const before = (await api('/api/channels')).channels;
    const failedPid = before.find(channel => channel.id === added.id).pid;
    const unaffectedPid = before.find(channel => channel.id === 'demo').pid;
    process.kill(failedPid);
    const recovered = await waitFor(async () => {
      const channels = (await api('/api/channels')).channels;
      assert.equal(channels.find(channel => channel.id === 'demo').pid, unaffectedPid);
      assert.equal(channels.find(channel => channel.id === 'demo').ready, true);
      const channel = channels.find(channel => channel.id === added.id);
      return channel.ready && channel.pid !== failedPid ? channel : null;
    });
    assert.notEqual(recovered.pid, failedPid);
    await api(`/api/channels/${added.id}/stop`, {});
    await waitFor(async () => !(await api('/api/channels')).channels.find(channel => channel.id === added.id).ready);
    const persisted = JSON.parse(await readFile(path.join(dataDir, 'channels.json')));
    assert.equal(persisted.find(channel => channel.id === added.id).enabled, false);
    const browser = spawn(process.execPath, ['scripts/verify-browser.js'], { stdio: 'inherit' });
    assert.equal(await new Promise(resolve => browser.on('exit', resolve)), 0);
    childPids = (await api('/api/channels')).channels.map(channel => channel.pid).filter(Boolean);
    await terminate();
    for (const pid of childPids) await waitFor(() => { try { process.kill(pid, 0); return false; } catch { return true; } });
    launch();
    await waitFor(async () => {
      const channels = (await api('/api/channels')).channels;
      return channels.find(channel => channel.id === 'demo')?.ready && channels.find(channel => channel.id === added.id)?.enabled === false;
    });
  } finally {
    if (gateway && gateway.exitCode === null) await terminate();
  }
});
