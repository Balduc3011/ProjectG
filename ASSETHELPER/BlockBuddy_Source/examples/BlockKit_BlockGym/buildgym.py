s=open('gym.src.html').read()
def rep(a,b):
    global s
    assert s.count(a)==1,a[:60]; s=s.replace(a,b)
rep('''  // ---------- camera ----------''', open('gym_chars.js').read() + '''
  // ---------- camera ----------''')
s=s.replace('__OLDMAN__',open('skin.b64').read().strip()).replace('__GREEN__',open('green.b64').read().strip()).replace('__CYAN__',open('cyan.b64').read().strip())
open('block-gym.html','w').write(s)
t='<!doctype html><html><head><meta charset=utf-8><meta name=viewport content="width=device-width"></head><body>'+s+'</body></html>'
t=t.replace('const view = { yaw','const view = window.__view = { yaw').replace('  new ResizeObserver(resize).observe(stage);','  window.__resize = resize; new ResizeObserver(resize).observe(stage);')
t=t.replace('const MODELS = {}, props = [],','const MODELS = {}, props = window.__props = [],').replace('  const actors = [];','  const actors = window.__actors = []; window.__jump = (idx, i, tt) => { const a = actors[idx]; a.i = i; a.t = tt; a.blend = 1; a.prev = null; };')
t=t.replace('const dt = Math.min(0.05, (now - last) / 1000); last = now; t += dt;','let dt = Math.min(0.05, (now - last) / 1000); last = now; if (window.__paused) dt = 0; t += dt;')
open('gym_test.html','w').write(t)
