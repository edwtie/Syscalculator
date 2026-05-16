const stage = document.getElementById('filmStage');
const caption = document.getElementById('filmCaption');
const code = document.getElementById('filmCode');
let runId = 0;

function clearStage() {
  runId++;
  stage.innerHTML = '';
  return runId;
}

function actor(text, x, y, cls = '') {
  const el = document.createElement('span');
  el.className = 'actor ' + cls;
  el.textContent = text;
  el.style.setProperty('--x', x + 'px');
  el.style.setProperty('--y', y + 'px');
  stage.appendChild(el);
  requestAnimationFrame(() => el.classList.add('show'));
  return el;
}

function move(el, x, y, scale = 1) {
  el.style.left = x + 'px';
  el.style.top = y + 'px';
  el.style.transform = 'scale(' + scale + ')';
}

function text(el, value) {
  el.textContent = value;
}

function setCaption(value) {
  caption.textContent = value;
}

function wait(ms, id) {
  return new Promise(resolve => setTimeout(() => resolve(id === runId), ms));
}

function showPowerStart(n) {
  clearStage();
  code.textContent = 'FormulaFilm.Generate("diff x^' + n + '")\nclick Generate to play the steps slowly';
  setCaption('Start frame: first look carefully at the function f(x) = x^' + n + '.');
  actor('f', 24, 58);
  actor('(', 44, 58);
  actor('x', 57, 58);
  actor(')', 72, 58);
  actor('=', 96, 58);
  actor('x', 134, 58);
  actor(String(n), 151, 47, 'small red');
}

async function playPower(n) {
  const id = clearStage();
  const next = n - 1;
  code.textContent = 'FormulaFilm.Generate("diff x^' + n + '")\nactors: f, x, exponent ' + n + ', coefficient ' + n + ', exponent ' + next;
  setCaption('Start frame: first look carefully at the function f(x) = x^' + n + '.');

  actor('f', 24, 58);
  actor('(', 44, 58);
  actor('x', 57, 58);
  actor(')', 72, 58);
  actor('=', 96, 58);
  const x = actor('x', 134, 58);
  const oldExp = actor(String(n), 151, 47, 'small gray memory');
  const exp = actor(String(n), 151, 47, 'small red');
  if (!await wait(1900, id)) return;

  setCaption('The power ' + n + ' moves forward and becomes the coefficient.');
  const prime = actor("'", 36, 47, 'red');
  if (!await wait(420, id)) return;
  prime.classList.remove('red');
  oldExp.classList.add('visible');
  exp.classList.remove('small');
  move(exp, 126, 58, 1);
  move(x, 158, 58, 1);
  move(oldExp, 175, 47, 1);
  if (!await wait(1500, id)) return;

  setCaption('The old power becomes smaller: ' + n + ' - 1 = ' + next + '.');
  oldExp.classList.remove('gray');
  oldExp.classList.add('red');
  text(oldExp, n + ' - 1');
  if (!await wait(900, id)) return;
  text(oldExp, String(next));
  if (!await wait(850, id)) return;

  if (next === 1) {
    setCaption('x\u00b9 is just x. The 1 disappears, but x stays in the formula.');
    oldExp.classList.add('pop');
    move(exp, 126, 58, 1);
    move(x, 158, 58, 1);
    if (!await wait(850, id)) return;
    setCaption("Final frame: f'(x) = " + n + 'x');
  } else {
    setCaption('The power stays visible: this becomes ' + n + 'x^' + next + '.');
    move(oldExp, 170, 47, 1);
    move(x, 154, 58, 1);
    if (!await wait(850, id)) return;
    setCaption("Final frame: f'(x) = " + n + 'x^' + next);
  }
}

async function playChainPreview() {
  const id = clearStage();
  code.textContent = 'FormulaFilm.Generate("diff (x^2 - 1)^3")\nactors: outer exponent, inner group, constant, inner derivative';
  setCaption('The chain rule is 2.1 research: outer function and inner function become separate groups.');
  actor('f', 20, 58);
  actor('(x)', 50, 58);
  actor('=', 100, 58);
  const openBlock = actor('(', 136, 58);
  const blockX = actor('x', 154, 58);
  const blockInnerExp = actor('2', 169, 47, 'small');
  const blockMinus = actor('- 1', 184, 58);
  const closeBlock = actor(')', 224, 58);
  const outerOldExp = actor('3', 242, 47, 'small gray memory');
  const exp = actor('3', 242, 47, 'small red');
  if (!await wait(1900, id)) return;
  const prime = actor("'", 32, 47, 'red');
  setCaption("We are finding the derivative: f(x) becomes f'(x). Then the outer power 3 moves forward.");
  if (!await wait(650, id)) return;
  prime.classList.remove('red');
  setCaption('Outer power 3 first moves forward above the group, so it stays the outer power.');
  outerOldExp.classList.add('visible');
  exp.classList.remove('small');
  move(exp, 198, 30, .78);
  if (!await wait(760, id)) return;
  setCaption('Now the outer power becomes the coefficient for the whole inner block.');
  move(exp, 130, 60, .82);
  move(openBlock, 154, 58, 1);
  move(blockX, 172, 58, 1);
  move(blockInnerExp, 187, 47, 1);
  move(blockMinus, 202, 58, 1);
  move(closeBlock, 242, 58, 1);
  move(outerOldExp, 260, 47, 1);
  if (!await wait(1400, id)) return;
  exp.classList.remove('red');
  setCaption('The outer power counts down: 3 - 1 becomes 2.');
  outerOldExp.classList.remove('gray');
  outerOldExp.classList.add('red');
  text(outerOldExp, '3 - 1');
  if (!await wait(850, id)) return;
  text(outerOldExp, '2');
  if (!await wait(700, id)) return;
  outerOldExp.classList.remove('red');
  const innerX = actor('x', 172, 58, 'ink');
  const innerOldExp = actor('2', 187, 47, 'small gray memory');
  const innerMovingExp = actor('2', 187, 47, 'small ink');
  const innerConstant = actor('- 1', 202, 58, 'ink');
  setCaption('Now we take the inner function separately: x\u00b2 - 1 comes out of the main block.');
  if (!await wait(250, id)) return;
  move(innerX, 210, 128, .72);
  move(innerOldExp, 222, 121, .86);
  move(innerMovingExp, 222, 121, .86);
  move(innerConstant, 246, 128, .72);
  if (!await wait(1050, id)) return;
  setCaption('First the constant -1 drops away.');
  innerConstant.classList.remove('ink');
  innerConstant.classList.add('red');
  if (!await wait(420, id)) return;
  innerConstant.classList.add('pop');
  if (!await wait(750, id)) return;
  innerConstant.remove();
  setCaption('Now x\u00b2 remains: this is the same basis as the Generate x\u00b2 button.');
  if (!await wait(750, id)) return;
  setCaption('Then the power 2 moves forward as the coefficient.');
  innerOldExp.classList.add('visible');
  innerMovingExp.classList.remove('ink');
  innerMovingExp.classList.add('red');
  innerMovingExp.classList.remove('small');
  move(innerMovingExp, 202, 128, .72);
  move(innerX, 220, 128, .72);
  move(innerOldExp, 232, 121, .86);
  if (!await wait(1100, id)) return;
  innerMovingExp.classList.remove('red');
  setCaption('The old power counts down: 2 - 1 becomes 1.');
  innerOldExp.classList.remove('gray');
  innerOldExp.classList.add('red');
  text(innerOldExp, '2 - 1');
  if (!await wait(800, id)) return;
  text(innerOldExp, '1');
  setCaption('2 - 1 becomes 1.');
  if (!await wait(360, id)) return;
  setCaption('We do not write x\u00b9, so the small power disappears.');
  innerOldExp.classList.add('pop');
  if (!await wait(520, id)) return;
  innerOldExp.remove();
  setCaption('Now x\u00b2 remains; with the power rule that becomes 2x.');
  if (!await wait(1100, id)) return;
  const dot = actor('\u00b7', 278, 58);
  innerX.classList.remove('ink');
  innerX.classList.add('red');
  innerMovingExp.classList.add('red');
  move(innerMovingExp, 314, 58, 1);
  move(innerX, 333, 58, 1);
  if (!await wait(520, id)) return;
  innerMovingExp.classList.remove('red');
  innerX.classList.remove('red');
  move(outerOldExp, 254, 47, 1);
  move(dot, 276, 58, 1);
  setCaption("Preview final frame: f'(x) = 3(x\u00b2 - 1)\u00b2 \u00b7 2x. The constant -1 has dropped away.");
}

window.addEventListener('DOMContentLoaded', () => {
  window.setTimeout(() => showPowerStart(2), 350);
});

document.addEventListener('click', event => {
  const target = event.target instanceof Element ? event.target : event.target.parentElement;
  const powerButton = target ? target.closest('[data-film-power]') : null;
  if (powerButton) {
    event.preventDefault();
    playPower(Number(powerButton.getAttribute('data-film-power')));
    return;
  }

  if (target && target.closest('[data-film-chain]')) {
    event.preventDefault();
    playChainPreview();
  }
});
