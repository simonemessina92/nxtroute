import { chromium } from 'playwright';
import { mkdir, writeFile } from 'node:fs/promises';

const browser = await chromium.launch({
  executablePath: process.env.BROWSER_PATH || undefined,
  channel: process.env.BROWSER_PATH ? undefined : 'msedge',
  headless: true,
  args: ['--autoplay-policy=no-user-gesture-required']
});
const results = { browser: browser.version(), observedAt: new Date().toISOString(), streams: [] };
try {
  for (const [protocol, url] of [
    ['WebRTC/WHEP', 'http://127.0.0.1:8889/demo/webrtc'],
    ['HLS', 'http://127.0.0.1:8888/demo/hls']
  ]) {
    const page = await browser.newPage();
    await page.addInitScript(() => {
      window.testConnections = [];
      const OriginalConnection = window.RTCPeerConnection;
      window.RTCPeerConnection = class extends OriginalConnection {
        constructor(...args) { super(...args); window.testConnections.push(this); }
      };
    });
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    const startedAt = Date.now();
    await page.goto(url);
    await page.waitForFunction(() => {
      const video = document.querySelector('video');
      return video && video.videoWidth > 0 && video.currentTime > 2 && !video.paused;
    }, undefined, { timeout: 45000 });
    const startupMilliseconds = Date.now() - startedAt;
    const start = await page.locator('video').evaluate(video => ({ time: video.currentTime, frames: video.getVideoPlaybackQuality().totalVideoFrames }));
    await page.waitForTimeout(5000);
    const sample = await page.locator('video').evaluate(video => ({
      time: video.currentTime,
      width: video.videoWidth,
      height: video.videoHeight,
      frames: video.getVideoPlaybackQuality().totalVideoFrames,
      dropped: video.getVideoPlaybackQuality().droppedVideoFrames,
      muted: video.muted,
      audioTracks: video.srcObject?.getAudioTracks().map(track => ({ enabled: track.enabled, state: track.readyState })) ?? null,
      liveEdgeDistanceSeconds: video.seekable.length ? video.seekable.end(video.seekable.length - 1) - video.currentTime : null
    }));
    if (sample.time <= start.time || sample.frames <= start.frames) throw new Error(`${protocol}: playback stalled`);
    const audio = await page.evaluate(async () => {
      const video = document.querySelector('video');
      const context = new AudioContext();
      await context.resume();
      video.muted = false;
      video.volume = 1;
      const source = video.srcObject ? context.createMediaStreamSource(video.srcObject) : context.createMediaElementSource(video);
      const analyser = context.createAnalyser();
      source.connect(analyser);
      analyser.connect(context.destination);
      await video.play();
      await new Promise(resolve => setTimeout(resolve, 1000));
      const samples = new Float32Array(analyser.fftSize);
      analyser.getFloatTimeDomainData(samples);
      const rms = Math.sqrt(samples.reduce((total, sample) => total + sample * sample, 0) / samples.length);
      await context.close();
      return { decodedAudioRms: rms };
    });
    if (audio.decodedAudioRms < 0.001) throw new Error(`${protocol}: no decoded audio detected`);
    const rtp = await page.evaluate(async () => {
      const result = [];
      for (const connection of window.testConnections) {
        const stats = await connection.getStats();
        stats.forEach(stat => {
          if (stat.type === 'inbound-rtp') result.push({ kind: stat.kind, bytesReceived: stat.bytesReceived, jitter: stat.jitter, averageJitterBufferSeconds: stat.jitterBufferEmittedCount ? stat.jitterBufferDelay / stat.jitterBufferEmittedCount : null, codec: stats.get(stat.codecId)?.mimeType });
        });
      }
      return result;
    });
    results.streams.push({ protocol, ...sample, ...audio, startupMilliseconds, rtp, advancedSeconds: sample.time - start.time, errors });
    await mkdir('test-results', { recursive: true });
    await page.screenshot({ path: `test-results/${protocol === 'HLS' ? 'hls' : 'webrtc'}.png` });
    await page.close();
  }
  console.log(JSON.stringify(results, null, 2));
  await writeFile('test-results/browser.json', JSON.stringify(results, null, 2));
} finally {
  await browser.close();
}
