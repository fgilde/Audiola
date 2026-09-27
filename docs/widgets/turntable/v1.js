var wt, V, Xi, Ie, ri, Ui, Wi, Dt, ft, at, $i, Vt, Gt, jt, bt = {}, vt = [], ma = /acit|ex(?:s|g|n|p|$)|rph|grid|ows|mnc|ntw|ine[ch]|zoo|^ord|itera/i, At = Array.isArray;
function Ce(t, e) {
  for (var i in e) t[i] = e[i];
  return t;
}
function Yt(t) {
  t && t.parentNode && t.parentNode.removeChild(t);
}
function ba(t, e, i) {
  var a, n, r, s = {};
  for (r in e) r == "key" ? a = e[r] : r == "ref" ? n = e[r] : s[r] = e[r];
  if (arguments.length > 2 && (s.children = arguments.length > 3 ? wt.call(arguments, 2) : i), typeof t == "function" && t.defaultProps != null) for (r in t.defaultProps) s[r] === void 0 && (s[r] = t.defaultProps[r]);
  return _t(t, s, a, n, null);
}
function _t(t, e, i, a, n) {
  var r = { type: t, props: e, key: i, ref: a, __k: null, __: null, __b: 0, __e: null, __c: null, constructor: void 0, __v: n ?? ++Xi, __i: -1, __u: 0 };
  return n == null && V.vnode != null && V.vnode(r), r;
}
function Ge(t) {
  return t.children;
}
function pt(t, e) {
  this.props = t, this.context = e;
}
function je(t, e) {
  if (e == null) return t.__ ? je(t.__, t.__i + 1) : null;
  for (var i; e < t.__k.length; e++) if ((i = t.__k[e]) != null && i.__e != null) return i.__e;
  return typeof t.type == "function" ? je(t) : null;
}
function va(t) {
  if (t.__P && t.__d) {
    var e = t.__v, i = e.__e, a = [], n = [], r = Ce({}, e);
    r.__v = e.__v + 1, V.vnode && V.vnode(r), Kt(t.__P, r, e, t.__n, t.__P.namespaceURI, 32 & e.__u ? [i] : null, a, i ?? je(e), !!(32 & e.__u), n), r.__v = e.__v, r.__.__k[r.__i] = r, Ji(a, r, n), e.__e = e.__ = null, r.__e != i && Vi(r);
  }
}
function Vi(t) {
  if ((t = t.__) != null && t.__c != null) return t.__e = t.__c.base = null, t.__k.some(function(e) {
    if (e != null && e.__e != null) return t.__e = t.__c.base = e.__e;
  }), Vi(t);
}
function si(t) {
  (!t.__d && (t.__d = !0) && Ie.push(t) && !xt.__r++ || ri != V.debounceRendering) && ((ri = V.debounceRendering) || Ui)(xt);
}
function xt() {
  try {
    for (var t, e = 1; Ie.length; ) Ie.length > e && Ie.sort(Wi), t = Ie.shift(), e = Ie.length, va(t);
  } finally {
    Ie.length = xt.__r = 0;
  }
}
function Yi(t, e, i, a, n, r, s, o, d, u, f) {
  var g, c, p, x, M, T, l = a && a.__k || vt, k = e.length;
  for (d = xa(i, e, l, d, k), g = 0; g < k; g++) (p = i.__k[g]) != null && (c = p.__i != -1 && l[p.__i] || bt, p.__i = g, T = Kt(t, p, c, n, r, s, o, d, u, f), x = p.__e, p.ref && c.ref != p.ref && (c.ref && Qt(c.ref, null, p), f.push(p.ref, p.__c || x, p)), M == null && x != null && (M = x), 4 & p.__u ? (d = Ki(p, d, t), c.__e && (c.__e = null)) : typeof p.type == "function" && T !== void 0 ? d = T : x && (d = x.nextSibling), p.__u &= -7);
  return i.__e = M, d;
}
function xa(t, e, i, a, n) {
  var r, s, o, d, u, f = i.length, g = f, c = 0;
  for (t.__k = new Array(n), r = 0; r < n; r++) (s = e[r]) != null && typeof s != "boolean" && typeof s != "function" ? (typeof s == "string" || typeof s == "number" || typeof s == "bigint" || s.constructor == String ? s = t.__k[r] = _t(null, s, null, null, null) : At(s) ? s = t.__k[r] = _t(Ge, { children: s }, null, null, null) : s.constructor === void 0 && s.__b > 0 ? s = t.__k[r] = _t(s.type, s.props, s.key, s.ref ? s.ref : null, s.__v) : t.__k[r] = s, d = r + c, s.__ = t, s.__b = t.__b + 1, o = null, (u = s.__i = ya(s, i, d, g)) != -1 && (g--, (o = i[u]) && (o.__u |= 2)), o == null || o.__v == null ? (u == -1 && (n > f ? c-- : n < f && c++), typeof s.type != "function" && (s.__u |= 4)) : u != d && (u == d - 1 ? c-- : u == d + 1 ? c++ : (u > d ? c-- : c++, s.__u |= 4))) : t.__k[r] = null;
  if (g) for (r = 0; r < f; r++) (o = i[r]) != null && !(2 & o.__u) && (o.__e == a && (a = je(o)), ea(o, o));
  return a;
}
function Ki(t, e, i) {
  var a, n;
  if (typeof t.type == "function") {
    for (a = t.__k, n = 0; a && n < a.length; n++) a[n] && (a[n].__ = t, e = Ki(a[n], e, i));
    return e;
  }
  t.__e != e && (e && t.type && !e.parentNode && (e = je(t)), e = i.insertBefore(t.__e, e || null));
  do
    e = e && e.nextSibling;
  while (e != null && e.nodeType == 8);
  return e;
}
function ya(t, e, i, a) {
  var n, r, s, o = t.key, d = t.type, u = e[i], f = u != null && (2 & u.__u) == 0;
  if (u === null && o == null || f && o == u.key && d == u.type) return i;
  if (a > (f ? 1 : 0)) {
    for (n = i - 1, r = i + 1; n >= 0 || r < e.length; ) if ((u = e[s = n >= 0 ? n-- : r++]) != null && !(2 & u.__u) && o == u.key && d == u.type) return s;
  }
  return -1;
}
function oi(t, e, i) {
  e[0] == "-" ? t.setProperty(e, i ?? "") : t[e] = i == null ? "" : typeof i != "number" || ma.test(e) ? i : i + "px";
}
function lt(t, e, i, a, n) {
  var r, s;
  e: if (e == "style") if (typeof i == "string") t.style.cssText = i;
  else {
    if (typeof a == "string" && (t.style.cssText = a = ""), a) for (e in a) i && e in i || oi(t.style, e, "");
    if (i) for (e in i) a && i[e] == a[e] || oi(t.style, e, i[e]);
  }
  else if (e[0] == "o" && e[1] == "n") r = e != (e = e.replace($i, "$1")), s = e.toLowerCase(), e = s in t || e == "onFocusOut" || e == "onFocusIn" ? s.slice(2) : e.slice(2), t.l || (t.l = {}), t.l[e + r] = i, i ? a ? i[at] = a[at] : (i[at] = Vt, t.addEventListener(e, r ? jt : Gt, r)) : t.removeEventListener(e, r ? jt : Gt, r);
  else {
    if (n == "http://www.w3.org/2000/svg") e = e.replace(/xlink(H|:h)/, "h").replace(/sName$/, "s");
    else if (e != "width" && e != "height" && e != "href" && e != "list" && e != "form" && e != "tabIndex" && e != "download" && e != "rowSpan" && e != "colSpan" && e != "role" && e != "popover" && e in t) try {
      t[e] = i ?? "";
      break e;
    } catch {
    }
    typeof i == "function" || (i == null || i === !1 && e[4] != "-" ? t.removeAttribute(e) : t.setAttribute(e, e == "popover" && i == 1 ? "" : i));
  }
}
function li(t) {
  return function(e) {
    if (this.l) {
      var i = this.l[e.type + t];
      if (e[ft] == null) e[ft] = Vt++;
      else if (e[ft] < i[at]) return;
      return i(V.event ? V.event(e) : e);
    }
  };
}
function Kt(t, e, i, a, n, r, s, o, d, u) {
  var f, g, c, p, x, M, T, l, k, y, v, q, N, I, H, G, F = e.type;
  if (e.constructor !== void 0) return null;
  128 & i.__u && (d = !!(32 & i.__u), r = [o = e.__e = i.__e]), (f = V.__b) && f(e);
  e: if (typeof F == "function") {
    g = s.length;
    try {
      if (k = e.props, y = F.prototype && F.prototype.render, v = (f = F.contextType) && a[f.__c], q = f ? v ? v.props.value : f.__ : a, i.__c ? l = (c = e.__c = i.__c).__ = c.__E : (y ? e.__c = c = new F(k, q) : (e.__c = c = new pt(k, q), c.constructor = F, c.render = wa), v && v.sub(c), c.state || (c.state = {}), c.__n = a, p = c.__d = !0, c.__h = [], c._sb = []), y && c.__s == null && (c.__s = c.state), y && F.getDerivedStateFromProps != null && (c.__s == c.state && (c.__s = Ce({}, c.__s)), Ce(c.__s, F.getDerivedStateFromProps(k, c.__s))), x = c.props, M = c.state, c.__v = e, p) y && F.getDerivedStateFromProps == null && c.componentWillMount != null && c.componentWillMount(), y && c.componentDidMount != null && c.__h.push(c.componentDidMount);
      else {
        if (y && F.getDerivedStateFromProps == null && k !== x && c.componentWillReceiveProps != null && c.componentWillReceiveProps(k, q), e.__v == i.__v || !c.__e && c.shouldComponentUpdate != null && c.shouldComponentUpdate(k, c.__s, q) === !1) {
          e.__v != i.__v && (c.props = k, c.state = c.__s, c.__d = !1), e.__e = i.__e, e.__k = i.__k, e.__k.some(function(h) {
            h && (h.__ = e);
          }), vt.push.apply(c.__h, c._sb), c._sb = [], c.__h.length && s.push(c), o = je(i);
          break e;
        }
        c.componentWillUpdate != null && c.componentWillUpdate(k, c.__s, q), y && c.componentDidUpdate != null && c.__h.push(function() {
          c.componentDidUpdate(x, M, T);
        });
      }
      if (c.context = q, c.props = k, c.__P = t, c.__e = !1, N = V.__r, I = 0, y) c.state = c.__s, c.__d = !1, N && N(e), f = c.render(c.props, c.state, c.context), vt.push.apply(c.__h, c._sb), c._sb = [];
      else do
        c.__d = !1, N && N(e), f = c.render(c.props, c.state, c.context), c.state = c.__s;
      while (c.__d && ++I < 25);
      c.state = c.__s, c.getChildContext != null && (a = Ce(Ce({}, a), c.getChildContext())), y && !p && c.getSnapshotBeforeUpdate != null && (T = c.getSnapshotBeforeUpdate(x, M)), H = f != null && f.type === Ge && f.key == null ? Zi(f.props.children) : f, o = Yi(t, At(H) ? H : [H], e, i, a, n, r, s, o, d, u), c.base = e.__e, e.__u &= -161, c.__h.length && s.push(c), l && (c.__E = c.__ = null);
    } catch (h) {
      if (s.length = g, e.__v = null, d || r != null) {
        if (h.then) {
          for (e.__u |= d ? 160 : 128; o && o.nodeType == 8 && o.nextSibling; ) o = o.nextSibling;
          r != null && (r[r.indexOf(o)] = null), e.__e = o;
        } else if (r != null) for (G = r.length; G--; ) Yt(r[G]);
      } else e.__e = i.__e;
      e.__k == null && (e.__k = i.__k || []), h.then || Qi(e), V.__e(h, e, i);
    }
  } else r == null && e.__v == i.__v ? (e.__k = i.__k, e.__e = i.__e) : o = e.__e = ka(i.__e, e, i, a, n, r, s, d, u);
  return (f = V.diffed) && f(e), 128 & e.__u ? void 0 : o;
}
function Qi(t) {
  t && (t.__c && (t.__c.__e = !0), t.__k && t.__k.some(Qi));
}
function Ji(t, e, i) {
  for (var a = 0; a < i.length; a++) Qt(i[a], i[++a], i[++a]);
  V.__c && V.__c(e, t), t.some(function(n) {
    try {
      t = n.__h, n.__h = [], t.some(function(r) {
        r.call(n);
      });
    } catch (r) {
      V.__e(r, n.__v);
    }
  });
}
function Zi(t) {
  return typeof t != "object" || t == null || t.__b > 0 ? t : At(t) ? t.map(Zi) : t.constructor !== void 0 ? null : Ce({}, t);
}
function ka(t, e, i, a, n, r, s, o, d) {
  var u, f, g, c, p, x, M, T = i.props || bt, l = e.props, k = e.type;
  if (k == "svg" ? n = "http://www.w3.org/2000/svg" : k == "math" ? n = "http://www.w3.org/1998/Math/MathML" : n || (n = "http://www.w3.org/1999/xhtml"), r != null) {
    for (u = 0; u < r.length; u++) if ((p = r[u]) && "setAttribute" in p == !!k && (k ? p.localName == k : p.nodeType == 3)) {
      t = p, r[u] = null;
      break;
    }
  }
  if (t == null) {
    if (k == null) return document.createTextNode(l);
    t = document.createElementNS(n, k, l.is && l), o && (V.__m && V.__m(e, r), o = !1), r = null;
  }
  if (k == null) T === l || o && t.data == l || (t.data = l);
  else {
    if (r = k == "textarea" && l.defaultValue != null ? null : r && wt.call(t.childNodes), !o && r != null) for (T = {}, u = 0; u < t.attributes.length; u++) T[(p = t.attributes[u]).name] = p.value;
    for (u in T) p = T[u], u == "dangerouslySetInnerHTML" ? g = p : u == "children" || u in l || u == "value" && "defaultValue" in l || u == "checked" && "defaultChecked" in l || lt(t, u, null, p, n);
    for (u in l) p = l[u], u == "children" ? c = p : u == "dangerouslySetInnerHTML" ? f = p : u == "value" ? x = p : u == "checked" ? M = p : o && typeof p != "function" || T[u] === p || lt(t, u, p, T[u], n);
    if (f) o || g && (f.__html == g.__html || f.__html == t.innerHTML) || (t.innerHTML = f.__html), e.__k = [];
    else if (g && (t.innerHTML = ""), Yi(e.type == "template" ? t.content : t, At(c) ? c : [c], e, i, a, k == "foreignObject" ? "http://www.w3.org/1999/xhtml" : n, r, s, r ? r[0] : i.__k && je(i, 0), o, d), r != null) for (u = r.length; u--; ) Yt(r[u]);
    o && k != "textarea" || (u = "value", k == "progress" && x == null ? t.removeAttribute("value") : x != null && (x !== t[u] || k == "progress" && !x || k == "option" && x != T[u]) && lt(t, u, x, T[u], n), u = "checked", M != null && M != t[u] && lt(t, u, M, T[u], n));
  }
  return t;
}
function Qt(t, e, i) {
  try {
    if (typeof t == "function") {
      var a = typeof t.__u == "function";
      a && t.__u(), a && e == null || (t.__u = t(e));
    } else t.current = e;
  } catch (n) {
    V.__e(n, i);
  }
}
function ea(t, e, i) {
  var a, n;
  if (V.unmount && V.unmount(t), (a = t.ref) && (a.current && a.current != t.__e || Qt(a, null, e)), (a = t.__c) != null) {
    if (a.componentWillUnmount) try {
      a.componentWillUnmount();
    } catch (r) {
      V.__e(r, e);
    }
    a.base = a.__P = a.__n = null;
  }
  if (a = t.__k) for (n = 0; n < a.length; n++) a[n] && ea(a[n], e, i || typeof t.type != "function");
  i || Yt(t.__e), t.__c = t.__ = t.__e = void 0;
}
function wa(t, e, i) {
  return this.constructor(t, i);
}
function ci(t, e, i) {
  var a, n, r, s;
  e == document && (e = document.documentElement), V.__ && V.__(t, e), n = (a = !1) ? null : e.__k, r = [], s = [], Kt(e, t = e.__k = ba(Ge, null, [t]), n || bt, bt, e.namespaceURI, n ? null : e.firstChild ? wt.call(e.childNodes) : null, r, n ? n.__e : e.firstChild, a, s), Ji(r, t, s), t.props.children = null;
}
wt = vt.slice, V = { __e: function(t, e, i, a) {
  for (var n, r, s; e = e.__; ) if ((n = e.__c) && !n.__) try {
    if ((r = n.constructor) && r.getDerivedStateFromError != null && (n.setState(r.getDerivedStateFromError(t)), s = n.__d), n.componentDidCatch != null && (n.componentDidCatch(t, a || {}), s = n.__d), s) return n.__E = n;
  } catch (o) {
    t = o;
  }
  throw t;
} }, Xi = 0, pt.prototype.setState = function(t, e) {
  var i;
  i = this.__s != null && this.__s != this.state ? this.__s : this.__s = Ce({}, this.state), typeof t == "function" && (t = t(Ce({}, i), this.props)), t && Ce(i, t), t != null && this.__v && (e && this._sb.push(e), si(this));
}, pt.prototype.forceUpdate = function(t) {
  this.__v && (this.__e = !0, t && this.__h.push(t), si(this));
}, pt.prototype.render = Ge, Ie = [], Ui = typeof Promise == "function" ? Promise.prototype.then.bind(Promise.resolve()) : setTimeout, Wi = function(t, e) {
  return t.__v.__b - e.__v.__b;
}, xt.__r = 0, Dt = Math.random().toString(8), ft = "__d" + Dt, at = "__a" + Dt, $i = /(PointerCapture)$|Capture$/i, Vt = 0, Gt = li(!1), jt = li(!0);
var Aa = 0;
function m(t, e, i, a, n, r) {
  e || (e = {});
  var s, o, d = e;
  if ("ref" in d) for (o in d = {}, e) o == "ref" ? s = e[o] : d[o] = e[o];
  var u = { type: t, props: d, key: i, ref: s, __k: null, __: null, __b: 0, __e: null, __c: null, constructor: void 0, __v: --Aa, __i: -1, __u: 0, __source: n, __self: r };
  if (typeof t == "function" && (s = t.defaultProps)) for (o in s) d[o] === void 0 && (d[o] = s[o]);
  return V.vnode && V.vnode(u), u;
}
var nt, te, It, di, yt = 0, ta = [], ae = V, hi = ae.__b, ui = ae.__r, fi = ae.diffed, _i = ae.__c, pi = ae.unmount, gi = ae.__;
function Jt(t, e) {
  ae.__h && ae.__h(te, t, yt || e), yt = 0;
  var i = te.__H || (te.__H = { __: [], __h: [] });
  return t >= i.__.length && i.__.push({}), i.__[t];
}
function He(t) {
  return yt = 1, La(aa, t);
}
function La(t, e, i) {
  var a = Jt(nt++, 2);
  if (a.t = t, !a.__c && (a.__ = [aa(void 0, e), function(o) {
    var d = a.__N ? a.__N[0] : a.__[0], u = a.t(d, o);
    d !== u && (a.__N = [u, a.__[1]], a.__c.setState({}));
  }], a.__c = te, !te.__f)) {
    var n = function(o, d, u) {
      if (!a.__c.__H) return !0;
      var f = !1, g = a.__c.props !== o;
      if (a.__c.__H.__.some(function(p) {
        if (p.__N) {
          f = !0;
          var x = p.__[0];
          p.__ = p.__N, p.__N = void 0, x !== p.__[0] && (g = !0);
        }
      }), r) {
        var c = r.call(this, o, d, u);
        return f ? c || g : c;
      }
      return !f || g;
    };
    te.__f = !0;
    var r = te.shouldComponentUpdate, s = te.componentWillUpdate;
    te.componentWillUpdate = function(o, d, u) {
      if (this.__e) {
        var f = r;
        r = void 0, n(o, d, u), r = f;
      }
      s && s.call(this, o, d, u);
    }, te.shouldComponentUpdate = n;
  }
  return a.__N || a.__;
}
function Ee(t, e) {
  var i = Jt(nt++, 3);
  !ae.__s && ia(i.__H, e) && (i.__ = t, i.u = e, te.__H.__h.push(i));
}
function ue(t) {
  return yt = 5, Ra(function() {
    return { current: t };
  }, []);
}
function Ra(t, e) {
  var i = Jt(nt++, 7);
  return ia(i.__H, e) && (i.__ = t(), i.__H = e, i.__h = t), i.__;
}
function Ca() {
  for (var t; t = ta.shift(); ) {
    var e = t.__H;
    if (t.__P && e) try {
      e.__h.some(gt), e.__h.some(Xt), e.__h = [];
    } catch (i) {
      e.__h = [], ae.__e(i, t.__v);
    }
  }
}
ae.__b = function(t) {
  te = null, hi && hi(t);
}, ae.__ = function(t, e) {
  t && e.__k && e.__k.__m && (t.__m = e.__k.__m), gi && gi(t, e);
}, ae.__r = function(t) {
  ui && ui(t), nt = 0;
  var e = (te = t.__c).__H;
  e && (It === te ? (e.__h = [], te.__h = [], e.__.some(function(i) {
    i.__N && (i.__ = i.__N), i.u = i.__N = void 0;
  })) : (e.__h.some(gt), e.__h.some(Xt), e.__h = [], nt = 0)), It = te;
}, ae.diffed = function(t) {
  fi && fi(t);
  var e = t.__c;
  e && e.__H && (e.__H.__h.length && (ta.push(e) !== 1 && di === ae.requestAnimationFrame || ((di = ae.requestAnimationFrame) || Ea)(Ca)), e.__H.__.some(function(i) {
    i.u && (i.__H = i.u, i.u = void 0);
  })), It = te = null;
}, ae.__c = function(t, e) {
  e.some(function(i) {
    try {
      i.__h.some(gt), i.__h = i.__h.filter(function(a) {
        return !a.__ || Xt(a);
      });
    } catch (a) {
      e.some(function(n) {
        n.__h && (n.__h = []);
      }), e = [], ae.__e(a, i.__v);
    }
  }), _i && _i(t, e);
}, ae.unmount = function(t) {
  pi && pi(t);
  var e, i = t.__c;
  i && i.__H && (i.__H.__.some(function(a) {
    try {
      gt(a);
    } catch (n) {
      e = n;
    }
  }), i.__H = void 0, e && ae.__e(e, i.__v));
};
var mi = typeof requestAnimationFrame == "function";
function Ea(t) {
  var e, i = function() {
    clearTimeout(a), mi && cancelAnimationFrame(e), setTimeout(t);
  }, a = setTimeout(i, 35);
  mi && (e = requestAnimationFrame(i));
}
function gt(t) {
  var e = te, i = t.__c;
  typeof i == "function" && (t.__c = void 0, i()), te = e;
}
function Xt(t) {
  var e = te;
  t.__c = t.__(), te = e;
}
function ia(t, e) {
  return !t || t.length !== e.length || e.some(function(i, a) {
    return i !== t[a];
  });
}
function aa(t, e) {
  return typeof e == "function" ? e(t) : e;
}
const Sa = `
class Deck extends AudioWorkletProcessor {
  static get parameterDescriptors() { return [{ name: 'rate', defaultValue: 0 }] }
  constructor() {
    super()
    this.L = null; this.R = null; this.pos = 0; this.gen = 0; this.tick = 0
    this.port.onmessage = (e) => {
      const d = e.data
      if (d.L) { this.L = d.L; this.R = d.R; this.gen = d.gen }
      if (d.seek !== undefined) this.pos = d.seek * sampleRate
    }
  }
  process(_, outputs, params) {
    const oL = outputs[0][0], oR = outputs[0][1], r = params.rate, L = this.L, R = this.R
    if (!L) return true
    const n = L.length
    for (let i = 0; i < oL.length; i++) {
      const p = this.pos
      if (p >= 0 && p < n - 1) {
        const i0 = p | 0, f = p - i0
        oL[i] = L[i0] + (L[i0 + 1] - L[i0]) * f
        oR[i] = R[i0] + (R[i0 + 1] - R[i0]) * f
      } else { oL[i] = 0; oR[i] = 0 }
      this.pos = Math.max(0, p + (r.length > 1 ? r[i] : r[0]))
    }
    if (++this.tick % 8 === 0) this.port.postMessage({ gen: this.gen, pos: this.pos / sampleRate, ended: this.pos >= n - 1 })
    return true
  }
}
registerProcessor('dr-deck', Deck)
`;
class bi {
  constructor(e) {
    this.engine = e;
  }
  ctx;
  node;
  eq;
  filter;
  fader;
  xf;
  analyser;
  meterBuf = new Float32Array(512);
  gen = 0;
  onPosition = () => {
  };
  /** Baut die Kanal-Kette auf; ruft die Engine nach dem Laden des Worklets auf. */
  attach(e, i) {
    this.ctx = e;
    const a = this.node = new AudioWorkletNode(e, "dr-deck", { outputChannelCount: [2] });
    a.port.onmessage = (n) => n.data.gen === this.gen && this.onPosition(n.data.pos, n.data.ended), this.eq = {
      low: new BiquadFilterNode(e, { type: "lowshelf", frequency: 220 }),
      mid: new BiquadFilterNode(e, { type: "peaking", frequency: 1e3, Q: 0.9 }),
      high: new BiquadFilterNode(e, { type: "highshelf", frequency: 3500 })
    }, this.filter = new BiquadFilterNode(e, { type: "allpass" }), this.fader = new GainNode(e), this.xf = new GainNode(e), this.analyser = new AnalyserNode(e, { fftSize: 512 }), a.connect(this.eq.low).connect(this.eq.mid).connect(this.eq.high).connect(this.filter).connect(this.fader).connect(this.xf).connect(i), this.xf.connect(this.analyser);
  }
  /** Kanalsignal nach Fader + Crossfader (für Ring-Visual); erst nach `unlock()` gesetzt. */
  get output() {
    return this.xf;
  }
  /**
   * Lädt einen Track und springt auf `offset(dauer)` Sekunden. Liefert die Dauer oder null,
   * wenn inzwischen ein anderer Track angefordert wurde.
   */
  async load(e, i) {
    const a = ++this.gen;
    await this.engine.unlock();
    const n = await fetch(e);
    if (!n.ok) throw new Error(`${n.status} ${e}`);
    const r = await this.ctx.decodeAudioData(await n.arrayBuffer());
    if (a !== this.gen) return null;
    const s = r.getChannelData(0).slice(), o = r.getChannelData(r.numberOfChannels > 1 ? 1 : 0).slice();
    return this.node.port.postMessage({ L: s, R: o, gen: a, seek: i(r.duration) }, [s.buffer, o.buffer]), r.duration;
  }
  seek(e) {
    this.node?.port.postMessage({ seek: e });
  }
  setRate(e) {
    this.node && this.node.parameters.get("rate").setTargetAtTime(e, this.ctx.currentTime, 0.01);
  }
  /** -1 = Kill, 0 = neutral, 1 = +6 dB. */
  setEq(e, i) {
    this.eq && (this.eq[e].gain.value = i < 0 ? i * 26 : i * 6);
  }
  /** DJ-Filter: -1 = Lowpass zu, 0 = neutral, 1 = Highpass zu. */
  setFilter(e) {
    const i = this.filter;
    if (i) {
      if (Math.abs(e) < 0.03) {
        i.type = "allpass";
        return;
      }
      i.type = e < 0 ? "lowpass" : "highpass", i.frequency.value = e < 0 ? 2e4 * 2 ** (e * 10) : 20 * 2 ** (e * 10), i.Q.value = 1 + Math.abs(e) * 6;
    }
  }
  setVolume(e) {
    this.fader && this.fader.gain.setTargetAtTime(e * e, this.ctx.currentTime, 0.01);
  }
  setCrossGain(e) {
    this.xf && this.xf.gain.setTargetAtTime(e, this.ctx.currentTime, 0.01);
  }
  /** Pegel 0…1 für VU-Meter. */
  level() {
    if (!this.analyser) return 0;
    this.analyser.getFloatTimeDomainData(this.meterBuf);
    let e = 0;
    for (const i of this.meterBuf) e += i * i;
    return Math.min(1, Math.sqrt(e / this.meterBuf.length) * 3.2);
  }
  /** Alles auf neutral (beim Schließen des Mischpults). */
  reset() {
    for (const e of ["low", "mid", "high"]) this.setEq(e, 0);
    this.setFilter(0), this.setVolume(1);
  }
}
class Ta {
  ctx;
  out;
  echoSend;
  ready;
  channels = [new bi(this), new bi(this)];
  /** Erster Aufruf muss aus einer Nutzer-Geste kommen (Autoplay-Policy). */
  unlock() {
    if (!this.ready) {
      const e = this.ctx = new AudioContext(), i = URL.createObjectURL(new Blob([Sa], { type: "text/javascript" }));
      this.ready = e.audioWorklet.addModule(i).then(() => {
        const a = new GainNode(e), n = this.out = new GainNode(e), r = this.echoSend = new GainNode(e, { gain: 0 }), s = new DelayNode(e, { delayTime: 0.375 }), o = new GainNode(e, { gain: 0.55 });
        a.connect(n).connect(e.destination), a.connect(r).connect(s).connect(o).connect(s), s.connect(n), this.channels.forEach((d) => d.attach(e, a));
      });
    }
    return this.ctx.resume(), this.ready;
  }
  /** Master-Ausgang für Analyzer; erst nach `unlock()` gesetzt. */
  get output() {
    return this.out;
  }
  /** -1 = nur A, 0 = beide voll, 1 = nur B (DJ-Kurve ohne Pegelloch in der Mitte). */
  setCrossfader(e) {
    const [i, a] = this.channels;
    i.setCrossGain(e <= 0 ? 1 : Math.cos(e * Math.PI / 2)), a.setCrossGain(e >= 0 ? 1 : Math.cos(-e * Math.PI / 2));
  }
  /** Echo-Send auf/zu; der Nachhall läuft nach dem Loslassen über das Feedback aus. */
  echo(e) {
    this.echoSend && this.echoSend.gain.setTargetAtTime(e ? 0.8 : 0, this.ctx.currentTime, 0.02);
  }
  /** Synthetisches Airhorn: gestapelte Sägezähne im klassischen „BAAP-BAP-BAP-BAAAP“-Rhythmus. */
  horn() {
    const e = this.ctx;
    if (!e || !this.out) return;
    const i = e.currentTime + 0.01, a = new BiquadFilterNode(e, { type: "lowpass", frequency: 2800, Q: 2 }), n = new GainNode(e, { gain: 0 });
    a.connect(n).connect(this.out);
    const r = [[0, 0.16], [0.2, 0.1], [0.34, 0.1], [0.48, 0.55]];
    for (const [s, o] of r)
      n.gain.setTargetAtTime(0.28, i + s, 8e-3), n.gain.setTargetAtTime(0, i + s + o, 0.02);
    for (const s of [466, 470, 700]) {
      const o = new OscillatorNode(e, { type: "sawtooth", frequency: s });
      o.frequency.setValueAtTime(s, i + 0.7), o.frequency.linearRampToValueAtTime(s * 0.94, i + 1.05), o.connect(a), o.start(i), o.stop(i + 1.2);
    }
  }
  /** Mischpult geschlossen: Crossfader Mitte, Kanäle neutral. */
  reset() {
    this.setCrossfader(0), this.channels.forEach((e) => e.reset());
  }
  close() {
    this.ctx?.close();
  }
}
const Ba = {
  pick: "Platte wählen",
  hint: "Arm auflegen · Platte drehen zum Scratchen",
  deck: "Deck",
  loading: "lädt …",
  error: "Laden fehlgeschlagen",
  prev: "Vorheriger Track",
  next: "Nächster Track",
  play: "Abspielen",
  pause: "Pause",
  open: "⇄ Mischpult öffnen",
  close: "✕ Mischpult schließen",
  pitch: "− Pitch ±8 % +",
  cueSet: "Cue setzen",
  cueJump: "Springen · Doppelklick löscht",
  spin: "Spin",
  upload: "Eigene Songs öffnen",
  drop: "Songs hier ablegen",
  myMusic: "Meine Musik",
  volume: "Lautstärke",
  crossfader: "A ◂ Crossfader ▸ B",
  poweredBy: "powered by"
}, Ma = {
  pick: "Pick a record",
  hint: "Drop the arm · spin the record to scratch",
  deck: "Deck",
  loading: "loading …",
  error: "Could not load",
  prev: "Previous track",
  next: "Next track",
  play: "Play",
  pause: "Pause",
  open: "⇄ Open mixer",
  close: "✕ Close mixer",
  pitch: "− Pitch ±8 % +",
  cueSet: "Set cue",
  cueJump: "Jump · double-click clears",
  spin: "Spin",
  upload: "Open your own songs",
  drop: "Drop songs here",
  myMusic: "My music",
  volume: "Volume",
  crossfader: "A ◂ Crossfader ▸ B",
  poweredBy: "powered by"
}, Da = (t) => (t ?? navigator.language).toLowerCase().startsWith("de") ? Ba : Ma, Ia = "data:image/webp;base64,UklGRiQIAABXRUJQVlA4WAoAAAAQAAAAPwAAPwAAQUxQSEMDAAABoEXbtilJ2ueem0Y4HRll27ZttW3btm3btm3bdpcd991zGpE58mX89eiPiJgA/MeZAMorxu6HgvOITOnPa6KG8sficNWTwXljqH619+syZPKFca06pzeA84QxSAKRwA8E58tz6kScPpcnjPnqREQCnQ/OAzLFn0sgqhLIF8WGWp/F/urkX+p0f9jWRATAmOSf3ufyfnnSGABErSMn4xx1KiIqok7PBaPVcv9akKFOm7yXpv2mTmQI9f2pFRAPSwMWd6qTZjq9HUxIDzYUHjAqg0IM1UD+raoiouIHgdF+LMLnbskBaVxc/5C6JkRFNq/Xp1BYUN830cWGRAWjGodlin67cbNKUyK6+oSsjs6Mb+jbMKqQwgH1iifRNlDJraoqEuhRX+lrBeWxLol+BmGS7Zbs0X7IlW/5QESb43X5Gq+Hde7So7FfalAhUYuBB1YPm7nFzGc1EHFNqajboN7rOXssGNErNbwQLUYdEm1i7csasIW6QLd/yQe5vP9ksfogmI6u0e6JzqkOaLF2sdqKulgy/q06XW+31U0iqiqb9Ghark5/TFTF0hXpeJuWS1fWlNfGcZg6p48W0H3qss5lnT5XgJvUOT0JiXRFQ0VNiwGmrqyhKP6n904P79ij8irNeWtt18wOGohfn7G1pfWMEKnEVuJM3ezcuu222HGqGXLag4+cPbJs3i5Ll/3pXFZvQjmXUBgAIb3aqerX20xuW8LIyaUdpu/4uapms/1gEDLjPtXfHjtmcBEAsGVjGQBKRp72wkrVN6yhcAw63HfwiCgAMpaZwUTWWDYEoHry0U8NgQkHKEBOg5yEptkSABQgbMa4x7uaS/rh4gEX3n/PIlzYM37dE7fUd7oUoIK7T0X4xjz1yQ1YPtf+MefXXbdcO+TTqfffO+zGFycst4XY4etvepAJiTFj1RbZxg9H4/NZ70Xxxv4PL/q5J6K/b/EOgB+PefJmcDhkij+757Q3brrx2aNW9lx92dW/ZL6ddNHn+71850g9dscDlp95kesJEw7iewDVO8cPOXswdjrmoAy2yWCnc/YtTO137K5bDgG2GBRSTovmEppvLcInSyAmaw2stQZMsNYSWctsADah/Q8jAFZQOCC6BAAAsBoAnQEqQABAAD4xEodDIiEKhzNyEAGCWwA0iPA78rY/wZkj/NPzAdpjxSOkB5gP146gHoAf0b+gdYB+wHsAeaj/vf3M+BP9p/22+Af9mv//rXJED1J8HXbz2oycz43QCf5N/eN4I49/K/9txgd5b+Vf5z0A7wbvT0Df7z2Sf5T/k+TX5u/6vuB/xr+e/6H+2fvH3afQn/VNzBBM9xPBL9vfpVmgJGY2Ylf6rPDYPaop45ctX9+5Yk1pCGa+FJrY90pBBy1Xb4+/6uUN2XR3bAuerktgjLtlSK7SZ3KAAP7/YCSv//pr2h/NpjzKYHzHrqzIe7wvV4CRniP9tgz6ts3zcL1VEA4VPF0kMrzA3j5zEPItpi6khSoifOGg53Fml1qHOwOkH02s0rMha1FY8VdGM6o4Behbm//1L95zlvDAowhwHOrqMtMB0oLuOAqiMqLj9/EAZCrw8hD7gjY3/93G0TQlHzFKUQb+zh6kX35taXWZPAQszQEp4gUNvUXPBmuaAmk9n5Cf3/Fh6mwW8/WXKzQ+0W7GC3wvlkvd+EtxDLRg0DKBDr6XnHywG5h10d4pP71dw+NpkKeoMJdrp8V/B4ZVWKav7+LsQzaSSETGablabdO+/7mHjIAlti5JvjqgOC8eLzLwN1hBfhKwIPS39u44WOXuBBW7c/hhyN+Sv7AVW9JQPXa+BDv/EuOmSEw7/vrkohIRqP2AfCAyQDurlyFeQS/fjMr2aPtlGsJDxbR8KYVixXcXttKGq7DtPhNVE1uhxo8IkzuCK62eoXAskrgD/osT6KDkXK9GAZ69l6lcNeeD0rGAG7qLmJm5PzjK83An3/N30wQRaqLVCfsrTNP/E3MBm1ppgA5e/P/Gaz4zpRLdAJevtWgqy5hHmYKSnNs4l6O3y6TjuFX4vsSMSIzs65WjlIq2DFqF+Ut2vlEEKUmWjweLVvZM8lfRoLtzJK//bZJJgkBEcuvaO3Wz9oTyccxRWU0Cz5yF4XMf/5TpzZXjlOEFIm9dH3hw5azGt/zP5KMScl+qVeuVqkKOw078teQ5KSop+yKc4d3e/J9iRlA16mdJr8JJ+8hRVwVbPAYiRXPlqB72Ax5TQmElnZ7phPOpX+e8viFOwuKPq5JUstTpyq93r+A+mkS/56jpY7Dwt8V2yecEvfjKJSFvjBTwBPEpE7FpoqNE12/IeaZKQ9uMXkIkU1WZ3LfgNOuPydJm/N1qvRyg4znsHhHJe+/3s73CCSVo2n+oaVtemKYzTuDU2IfoxyqEdkkYwgrga+IocQm70JWwutucmchFtULPs8Z/u3yLdtibk9vTciAqH9lAIelFMA8GYL//UvNmcLw2OPSSBXfasFbHwb+BvSfYS+tisTpOWKeX0GUTKj0jpt+CqdZuEg2pgfrrWLo1EHhyqTXm9JU46hDiUFOBBTi3W5d7RC0WS/NjYa7BPAKhM6diNB/sybHLX+E5phK/OQyhOSBt6WtVR5acwz0VZWy/pe720fDDne2vKayLkHa+utzKcqExmo0oHAsMAWf9PXzc+o5G/tqkqhdgoQaEKbeTcCYlBLUvxucgur3LrvmiHM4UQy/rxceDMX6XQI+VUr/eAAAAAA==";
function ct({ label: t, onChange: e }) {
  const [i, a] = He(0), n = ue(null), r = (o) => {
    const d = Math.max(-1, Math.min(1, Math.abs(o) < 0.04 ? 0 : o));
    a(d), e(d);
  }, s = (o) => {
    const d = { ArrowUp: 0.1, ArrowRight: 0.1, ArrowDown: -0.1, ArrowLeft: -0.1 }[o.key];
    d && (o.preventDefault(), r(i + d));
  };
  return /* @__PURE__ */ m("div", { class: "at-knob-wrap", children: [
    /* @__PURE__ */ m(
      "div",
      {
        class: "at-knob",
        role: "slider",
        tabIndex: 0,
        "aria-label": t,
        "aria-valuemin": -1,
        "aria-valuemax": 1,
        "aria-valuenow": Math.round(i * 100) / 100,
        style: { "--knob": `${i * 135}deg` },
        onPointerDown: (o) => {
          o.currentTarget.setPointerCapture(o.pointerId), n.current = { y: o.clientY, v: i };
        },
        onPointerMove: (o) => n.current && r(n.current.v + (n.current.y - o.clientY) / 90),
        onPointerUp: () => n.current = null,
        onPointerCancel: () => n.current = null,
        onDblClick: () => r(0),
        onKeyDown: s
      }
    ),
    /* @__PURE__ */ m("span", { children: t })
  ] });
}
function Pa(t) {
  const e = (i) => (a) => t.channel.setEq(i, a);
  return /* @__PURE__ */ m("div", { class: "at-mixer-strip", children: [
    /* @__PURE__ */ m("span", { class: "at-mixer-ch", children: t.name }),
    /* @__PURE__ */ m(ct, { label: "Hi", onChange: e("high") }),
    /* @__PURE__ */ m(ct, { label: "Mid", onChange: e("mid") }),
    /* @__PURE__ */ m(ct, { label: "Low", onChange: e("low") }),
    /* @__PURE__ */ m(ct, { label: "Filter", onChange: (i) => t.channel.setFilter(i) }),
    /* @__PURE__ */ m("div", { class: "at-mixer-faderbox", children: [
      /* @__PURE__ */ m("div", { class: "at-mixer-vu", children: /* @__PURE__ */ m("div", { ref: t.meter, class: "at-mixer-vu-fill" }) }),
      /* @__PURE__ */ m("div", { class: "at-fader at-mixer-vfader", children: /* @__PURE__ */ m(
        "input",
        {
          type: "range",
          min: 0,
          max: 1,
          step: 0.01,
          defaultValue: "1",
          "aria-label": `${t.t.volume} ${t.name}`,
          onInput: (i) => t.channel.setVolume(+i.currentTarget.value)
        }
      ) })
    ] })
  ] });
}
function Na({ engine: t, active: e, t: i, onClose: a }) {
  const n = ue([]);
  Ee(() => {
    if (!e) return;
    let s = 0;
    const o = () => {
      t.channels.forEach((d, u) => {
        const f = n.current[u];
        f && (f.style.transform = `scaleY(${d.level()})`);
      }), s = requestAnimationFrame(o);
    };
    return s = requestAnimationFrame(o), () => cancelAnimationFrame(s);
  }, [e, t]);
  const r = (s) => void t.unlock().then(() => t.echo(s));
  return /* @__PURE__ */ m("div", { class: "at-mixer", children: [
    /* @__PURE__ */ m("button", { class: "at-dj-toggle", onClick: a, children: i.close }),
    /* @__PURE__ */ m("div", { class: "at-mixer-strips", children: t.channels.map((s, o) => /* @__PURE__ */ m(Pa, { channel: s, name: o ? "B" : "A", t: i, meter: (d) => n.current[o] = d }, o)) }),
    /* @__PURE__ */ m("label", { class: "at-fader at-mixer-xfader", children: [
      /* @__PURE__ */ m("span", { children: i.crossfader }),
      /* @__PURE__ */ m(
        "input",
        {
          type: "range",
          min: -1,
          max: 1,
          step: 0.01,
          defaultValue: "0",
          onInput: (s) => t.setCrossfader(+s.currentTarget.value),
          onDblClick: (s) => {
            s.currentTarget.value = "0", t.setCrossfader(0);
          }
        }
      )
    ] }),
    /* @__PURE__ */ m("div", { class: "at-pads", children: [
      /* @__PURE__ */ m(
        "button",
        {
          class: "at-pad",
          onPointerDown: () => r(!0),
          onPointerUp: () => r(!1),
          onPointerLeave: () => r(!1),
          onPointerCancel: () => r(!1),
          onKeyDown: (s) => (s.key === " " || s.key === "Enter") && r(!0),
          onKeyUp: () => r(!1),
          children: "Echo"
        }
      ),
      /* @__PURE__ */ m("button", { class: "at-pad", onClick: () => void t.unlock().then(() => t.horn()), children: "Horn" })
    ] }),
    /* @__PURE__ */ m("a", { class: "at-powered", href: "https://audiola.de/turntable.html", target: "_blank", rel: "noopener", children: [
      i.poweredBy,
      " ",
      /* @__PURE__ */ m("img", { src: Ia, alt: "" }),
      " Audiola"
    ] })
  ] });
}
/**!
 * audioMotion-analyzer
 * High-resolution real-time graphic audio spectrum analyzer JS module
 *
 * @version 4.5.4
 * @author  Henrique Avila Vianna <hvianna@gmail.com> <https://henriquevianna.com>
 * @license AGPL-3.0-or-later
 */
const Fa = "4.5.4", Zt = Math.PI, We = 2 * Zt, Pt = Zt / 2, vi = 8.17579892, xi = "dual-combined", dt = "dual-horizontal", Re = "single", Ae = "dual-vertical", yi = "bar-index", ki = "bar-level", mt = "gradient", wi = 60, Ai = "click", Oa = "fullscreenchange", na = "resize", qa = "#111", ra = "", Li = "A", Ri = "B", Ci = "C", Ei = "D", Si = "468", ht = "sans-serif", za = "#0f0", Ha = "#7f7f7f22", $e = 10, Ti = "create", Bi = "fschange", Ga = "lores", ja = na, Nt = "user", Xa = "#000c", Mi = "#fff", Ua = "#4f4", Di = "#888", Wa = "#555", Ft = "bark", et = "linear", Ye = "log", Ot = "mel", Ii = ["#a35", "#c66", "#e94", "#ed0", "#9d5", "#4d8", "#2cb", "#0bc", "#09c", "#36b"], sa = [
  ["classic", {
    colorStops: [
      "red",
      { color: "yellow", level: 0.85, pos: 0.6 },
      { color: "lime", level: 0.475 }
    ]
  }],
  ["prism", {
    colorStops: Ii
  }],
  ["rainbow", {
    dir: "h",
    colorStops: ["#817", ...Ii, "#639"]
  }],
  ["orangered", {
    bgColor: "#3e2f29",
    colorStops: ["OrangeRed"]
  }],
  ["steelblue", {
    bgColor: "#222c35",
    colorStops: ["SteelBlue"]
  }]
], tt = {
  alphaBars: !1,
  ansiBands: !1,
  barSpace: 0.1,
  bgAlpha: 0.7,
  channelLayout: Re,
  colorMode: mt,
  fadePeaks: !1,
  fftSize: 8192,
  fillAlpha: 1,
  frequencyScale: Ye,
  gradient: sa[0][0],
  gravity: 3.8,
  height: void 0,
  ledBars: !1,
  linearAmplitude: !1,
  linearBoost: 1,
  lineWidth: 0,
  loRes: !1,
  lumiBars: !1,
  maxDecibels: -25,
  maxFPS: 0,
  maxFreq: 22e3,
  minDecibels: -85,
  minFreq: 20,
  mirror: 0,
  mode: 0,
  noteLabels: !1,
  outlineBars: !1,
  overlay: !1,
  peakFadeTime: 750,
  peakHoldTime: 500,
  peakLine: !1,
  radial: !1,
  radialInvert: !1,
  radius: 0.3,
  reflexAlpha: 0.15,
  reflexBright: 1,
  reflexFit: !0,
  reflexRatio: 0,
  roundBars: !1,
  showBgColor: !0,
  showFPS: !1,
  showPeaks: !0,
  showScaleX: !0,
  showScaleY: !1,
  smoothing: 0.5,
  spinSpeed: 0,
  splitGradient: !1,
  start: !0,
  trueLeds: !1,
  useCanvas: !0,
  volume: 1,
  weightingFilter: ra,
  width: void 0
}, $a = ["ERR_AUDIO_CONTEXT_FAIL", "Could not create audio context. Web Audio API not supported?"], Va = ["ERR_INVALID_AUDIO_CONTEXT", "Provided audio context is not valid"], Ya = ["ERR_UNKNOWN_GRADIENT", "Unknown gradient"], qt = ["ERR_FREQUENCY_TOO_LOW", "Frequency values must be >= 1"], Ka = ["ERR_INVALID_MODE", "Invalid mode"], Qa = ["ERR_REFLEX_OUT_OF_RANGE", "Reflex ratio must be >= 0 and < 1"], Ja = ["ERR_INVALID_AUDIO_SOURCE", "Audio source must be an instance of HTMLMediaElement or AudioNode"], Za = ["ERR_GRADIENT_INVALID_NAME", "Gradient name must be a non-empty string"], en = ["ERR_GRADIENT_NOT_AN_OBJECT", "Gradient options must be an object"], tn = ["ERR_GRADIENT_MISSING_COLOR", "Gradient colorStops must be a non-empty array"];
class me extends Error {
  constructor(e, i) {
    const [a, n] = e;
    super(n + (i !== void 0 ? `: ${i}` : "")), this.name = "AudioMotionError", this.code = a;
  }
}
const Pi = (t, e) => console.warn(`${t} is deprecated. Use ${e} instead.`), Ni = (t) => {
  for (const e in t)
    return !1;
  return !0;
}, ut = (t, e, i = "toLowerCase") => e[Math.max(0, e.indexOf(("" + t)[i]()))], an = (t, e, i, a, n) => e + (a - e) * (n - t) / (i - t);
Array.prototype.findLastIndex || (Array.prototype.findLastIndex = function(t) {
  let e = this.length;
  for (; e-- > 0; )
    if (t(this[e]))
      return e;
  return -1;
});
class nn {
  /**
   * CONSTRUCTOR
   *
   * @param {object} [container] DOM element where to insert the analyzer; if undefined, uses the document body
   * @param {object} [options]
   * @returns {object} AudioMotionAnalyzer object
   */
  constructor(e, i = {}) {
    this._ready = !1, this._aux = {}, this._canvasGradients = [], this._destroyed = !1, this._energy = { val: 0, peak: 0, hold: 0 }, this._flg = {}, this._fps = 0, this._gradients = {}, this._last = 0, this._outNodes = [], this._ownContext = !1, this._selectedGrads = [], this._sources = [], e instanceof Element || (Ni(i) && !Ni(e) && (i = e), e = null), this._ownCanvas = !(i.canvas instanceof HTMLCanvasElement);
    const a = this._ownCanvas ? document.createElement("canvas") : i.canvas;
    a.style = "max-width: 100%;", this._ctx = a.getContext("2d");
    for (const [g, c] of sa)
      this.registerGradient(g, c);
    this._container = e || !this._ownCanvas && a.parentElement || document.body, this._defaultWidth = this._container.clientWidth || 640, this._defaultHeight = this._container.clientHeight || 270;
    let n;
    if (!(i.source && (n = i.source.context))) {
      if (!(n = i.audioCtx)) try {
        n = new (window.AudioContext || window.webkitAudioContext)(), this._ownContext = !0;
      } catch {
        throw new me($a);
      }
    }
    if (!n.createGain)
      throw new me(Va);
    const r = this._analyzer = [n.createAnalyser(), n.createAnalyser()], s = this._splitter = n.createChannelSplitter(2), o = this._merger = n.createChannelMerger(2);
    this._input = n.createGain(), this._output = n.createGain(), i.source && this.connectInput(i.source);
    for (const g of [0, 1])
      s.connect(r[g], g);
    o.connect(this._output), i.connectSpeakers !== !1 && this.connectOutput();
    for (const g of ["_scaleX", "_scaleR"])
      this[g] = document.createElement("canvas").getContext("2d");
    this._fsEl = i.fsElement || a;
    const d = () => {
      this._fsTimeout || (this._fsTimeout = window.setTimeout(() => {
        this._fsChanging || (this._setCanvas(ja), this._fsTimeout = 0);
      }, wi));
    };
    window.ResizeObserver && (this._observer = new ResizeObserver(d), this._observer.observe(this._container)), this._controller = new AbortController();
    const u = this._controller.signal;
    window.addEventListener(na, d, { signal: u }), a.addEventListener(Oa, () => {
      this._fsChanging = !0, this._fsTimeout && window.clearTimeout(this._fsTimeout), this._setCanvas(Bi), this._fsTimeout = window.setTimeout(() => {
        this._fsChanging = !1, this._fsTimeout = 0;
      }, wi);
    }, { signal: u });
    const f = () => {
      n.state == "suspended" && n.resume().then(() => window.removeEventListener(Ai, f));
    };
    window.addEventListener(Ai, f), document.addEventListener("visibilitychange", () => {
      document.visibilityState != "hidden" && (this._frames = 0, this._time = performance.now());
    }, { signal: u }), this._setProps(i, !0), this.useCanvas && this._ownCanvas && this._container.appendChild(a), this._ready = !0, this._setCanvas(Ti);
  }
  /**
   * ==========================================================================
   *
   * PUBLIC PROPERTIES GETTERS AND SETTERS
   *
   * ==========================================================================
   */
  get alphaBars() {
    return this._alphaBars;
  }
  set alphaBars(e) {
    this._alphaBars = !!e, this._calcBars();
  }
  get ansiBands() {
    return this._ansiBands;
  }
  set ansiBands(e) {
    this._ansiBands = !!e, this._calcBars();
  }
  get barSpace() {
    return this._barSpace;
  }
  set barSpace(e) {
    this._barSpace = +e || 0, this._calcBars();
  }
  get channelLayout() {
    return this._chLayout;
  }
  set channelLayout(e) {
    this._chLayout = ut(e, [Re, dt, Ae, xi]), this._input.disconnect(), this._input.connect(this._chLayout != Re ? this._splitter : this._analyzer[0]), this._analyzer[0].disconnect(), this._outNodes.length && this._analyzer[0].connect(this._chLayout != Re ? this._merger : this._output), this._calcBars(), this._makeGrad();
  }
  get colorMode() {
    return this._colorMode;
  }
  set colorMode(e) {
    this._colorMode = ut(e, [mt, yi, ki]);
  }
  get fadePeaks() {
    return this._fadePeaks;
  }
  set fadePeaks(e) {
    this._fadePeaks = !!e;
  }
  get fftSize() {
    return this._analyzer[0].fftSize;
  }
  set fftSize(e) {
    for (const a of [0, 1])
      this._analyzer[a].fftSize = e;
    const i = this._analyzer[0].frequencyBinCount;
    this._fftData = [new Float32Array(i), new Float32Array(i)], this._calcBars();
  }
  get frequencyScale() {
    return this._frequencyScale;
  }
  set frequencyScale(e) {
    this._frequencyScale = ut(e, [Ye, Ft, Ot, et]), this._calcBars();
  }
  get gradient() {
    return this._selectedGrads[0];
  }
  set gradient(e) {
    this._setGradient(e);
  }
  get gradientLeft() {
    return this._selectedGrads[0];
  }
  set gradientLeft(e) {
    this._setGradient(e, 0);
  }
  get gradientRight() {
    return this._selectedGrads[1];
  }
  set gradientRight(e) {
    this._setGradient(e, 1);
  }
  get gravity() {
    return this._gravity;
  }
  set gravity(e) {
    this._gravity = e > 0 ? +e : this._gravity || tt.gravity;
  }
  get height() {
    return this._height;
  }
  set height(e) {
    this._height = e, this._setCanvas(Nt);
  }
  get ledBars() {
    return this._showLeds;
  }
  set ledBars(e) {
    this._showLeds = !!e, this._calcBars();
  }
  get linearAmplitude() {
    return this._linearAmplitude;
  }
  set linearAmplitude(e) {
    this._linearAmplitude = !!e;
  }
  get linearBoost() {
    return this._linearBoost;
  }
  set linearBoost(e) {
    this._linearBoost = e >= 1 ? +e : 1;
  }
  get lineWidth() {
    return this._lineWidth;
  }
  set lineWidth(e) {
    this._lineWidth = +e || 0;
  }
  get loRes() {
    return this._loRes;
  }
  set loRes(e) {
    this._loRes = !!e, this._setCanvas(Ga);
  }
  get lumiBars() {
    return this._lumiBars;
  }
  set lumiBars(e) {
    this._lumiBars = !!e, this._calcBars(), this._makeGrad();
  }
  get maxDecibels() {
    return this._analyzer[0].maxDecibels;
  }
  set maxDecibels(e) {
    for (const i of [0, 1])
      this._analyzer[i].maxDecibels = e;
  }
  get maxFPS() {
    return this._maxFPS;
  }
  set maxFPS(e) {
    this._maxFPS = e < 0 ? 0 : +e || 0;
  }
  get maxFreq() {
    return this._maxFreq;
  }
  set maxFreq(e) {
    if (e < 1)
      throw new me(qt);
    this._maxFreq = Math.min(e, this.audioCtx.sampleRate / 2), this._calcBars();
  }
  get minDecibels() {
    return this._analyzer[0].minDecibels;
  }
  set minDecibels(e) {
    for (const i of [0, 1])
      this._analyzer[i].minDecibels = e;
  }
  get minFreq() {
    return this._minFreq;
  }
  set minFreq(e) {
    if (e < 1)
      throw new me(qt);
    this._minFreq = +e, this._calcBars();
  }
  get mirror() {
    return this._mirror;
  }
  set mirror(e) {
    this._mirror = Math.sign(e) | 0, this._calcBars(), this._makeGrad();
  }
  get mode() {
    return this._mode;
  }
  set mode(e) {
    const i = e | 0;
    if (i >= 0 && i <= 10 && i != 9)
      this._mode = i, this._calcBars(), this._makeGrad();
    else
      throw new me(Ka, e);
  }
  get noteLabels() {
    return this._noteLabels;
  }
  set noteLabels(e) {
    this._noteLabels = !!e, this._createScales();
  }
  get outlineBars() {
    return this._outlineBars;
  }
  set outlineBars(e) {
    this._outlineBars = !!e, this._calcBars();
  }
  get peakFadeTime() {
    return this._peakFadeTime;
  }
  set peakFadeTime(e) {
    this._peakFadeTime = e >= 0 ? +e : this._peakFadeTime || tt.peakFadeTime;
  }
  get peakHoldTime() {
    return this._peakHoldTime;
  }
  set peakHoldTime(e) {
    this._peakHoldTime = +e || 0;
  }
  get peakLine() {
    return this._peakLine;
  }
  set peakLine(e) {
    this._peakLine = !!e;
  }
  get radial() {
    return this._radial;
  }
  set radial(e) {
    this._radial = !!e, this._calcBars(), this._makeGrad();
  }
  get radialInvert() {
    return this._radialInvert;
  }
  set radialInvert(e) {
    this._radialInvert = !!e, this._calcBars(), this._makeGrad();
  }
  get radius() {
    return this._radius;
  }
  set radius(e) {
    this._radius = +e || 0, this._calcBars(), this._makeGrad();
  }
  get reflexRatio() {
    return this._reflexRatio;
  }
  set reflexRatio(e) {
    if (e = +e || 0, e < 0 || e >= 1)
      throw new me(Qa);
    this._reflexRatio = e, this._calcBars(), this._makeGrad();
  }
  get roundBars() {
    return this._roundBars;
  }
  set roundBars(e) {
    this._roundBars = !!e, this._calcBars();
  }
  get smoothing() {
    return this._analyzer[0].smoothingTimeConstant;
  }
  set smoothing(e) {
    for (const i of [0, 1])
      this._analyzer[i].smoothingTimeConstant = e;
  }
  get spinSpeed() {
    return this._spinSpeed;
  }
  set spinSpeed(e) {
    e = +e || 0, (this._spinSpeed === void 0 || e == 0) && (this._spinAngle = -Pt), this._spinSpeed = e;
  }
  get splitGradient() {
    return this._splitGradient;
  }
  set splitGradient(e) {
    this._splitGradient = !!e, this._makeGrad();
  }
  get stereo() {
    return Pi("stereo", "channelLayout"), this._chLayout != Re;
  }
  set stereo(e) {
    Pi("stereo", "channelLayout"), this.channelLayout = e ? Ae : Re;
  }
  get trueLeds() {
    return this._trueLeds;
  }
  set trueLeds(e) {
    this._trueLeds = !!e;
  }
  get volume() {
    return this._output.gain.value;
  }
  set volume(e) {
    this._output.gain.value = e;
  }
  get weightingFilter() {
    return this._weightingFilter;
  }
  set weightingFilter(e) {
    this._weightingFilter = ut(e, [ra, Li, Ri, Ci, Ei, Si], "toUpperCase");
  }
  get width() {
    return this._width;
  }
  set width(e) {
    this._width = e, this._setCanvas(Nt);
  }
  // Read only properties
  get audioCtx() {
    return this._input.context;
  }
  get canvas() {
    return this._ctx.canvas;
  }
  get canvasCtx() {
    return this._ctx;
  }
  get connectedSources() {
    return this._sources;
  }
  get connectedTo() {
    return this._outNodes;
  }
  get fps() {
    return this._fps;
  }
  get fsHeight() {
    return this._fsHeight;
  }
  get fsWidth() {
    return this._fsWidth;
  }
  get isAlphaBars() {
    return this._flg.isAlpha;
  }
  get isBandsMode() {
    return this._flg.isBands;
  }
  get isDestroyed() {
    return this._destroyed;
  }
  get isFullscreen() {
    return this._fsEl && (document.fullscreenElement || document.webkitFullscreenElement) === this._fsEl;
  }
  get isLedBars() {
    return this._flg.isLeds;
  }
  get isLumiBars() {
    return this._flg.isLumi;
  }
  get isOctaveBands() {
    return this._flg.isOctaves;
  }
  get isOn() {
    return !!this._runId;
  }
  get isOutlineBars() {
    return this._flg.isOutline;
  }
  get pixelRatio() {
    return this._pixelRatio;
  }
  get isRoundBars() {
    return this._flg.isRound;
  }
  static get version() {
    return Fa;
  }
  /**
   * ==========================================================================
      *
   * PUBLIC METHODS
   *
   * ==========================================================================
   */
  /**
   * Connects an HTML media element or audio node to the analyzer
   *
   * @param {object} an instance of HTMLMediaElement or AudioNode
   * @returns {object} a MediaElementAudioSourceNode object if created from HTML element, or the same input object otherwise
   */
  connectInput(e) {
    const i = e instanceof HTMLMediaElement;
    if (!(i || e.connect))
      throw new me(Ja);
    const a = i ? this.audioCtx.createMediaElementSource(e) : e;
    return this._sources.includes(a) || (a.connect(this._input), this._sources.push(a)), a;
  }
  /**
   * Connects the analyzer output to another audio node
   *
   * @param [{object}] an AudioNode; if undefined, the output is connected to the audio context destination (speakers)
   */
  connectOutput(e = this.audioCtx.destination) {
    if (!this._outNodes.includes(e) && (this._output.connect(e), this._outNodes.push(e), this._outNodes.length == 1))
      for (const i of [0, 1])
        this._analyzer[i].connect(this._chLayout == Re && !i ? this._output : this._merger, 0, i);
  }
  /**
   * Destroys instance
   */
  destroy() {
    if (!this._ready)
      return;
    const { audioCtx: e, canvas: i, _controller: a, _input: n, _merger: r, _observer: s, _ownCanvas: o, _ownContext: d, _splitter: u } = this;
    this._destroyed = !0, this._ready = !1, this.stop(), a.abort(), s && s.disconnect(), this.onCanvasResize = null, this.onCanvasDraw = null, this._fsEl = null, this.disconnectInput(), this.disconnectOutput(), n.disconnect(), u.disconnect(), r.disconnect(), d && e.close(), o && i.remove(), this._calcBars();
  }
  /**
   * Disconnects audio sources from the analyzer
   *
   * @param [{object|array}] a connected AudioNode object or an array of such objects; if falsy, all connected nodes are disconnected
   * @param [{boolean}] if true, stops/releases audio tracks from disconnected media streams (e.g. microphone)
   */
  disconnectInput(e, i) {
    e ? Array.isArray(e) || (e = [e]) : e = Array.from(this._sources);
    for (const a of e) {
      const n = this._sources.indexOf(a);
      if (i && a.mediaStream)
        for (const r of a.mediaStream.getAudioTracks())
          r.stop();
      n >= 0 && (a.disconnect(this._input), this._sources.splice(n, 1));
    }
  }
  /**
   * Disconnects the analyzer output from other audio nodes
   *
   * @param [{object}] a connected AudioNode object; if undefined, all connected nodes are disconnected
   */
  disconnectOutput(e) {
    if (!(e && !this._outNodes.includes(e)) && (this._output.disconnect(e), this._outNodes = e ? this._outNodes.filter((i) => i !== e) : [], this._outNodes.length == 0))
      for (const i of [0, 1])
        this._analyzer[i].disconnect();
  }
  /**
   * Returns analyzer bars data
      *
   * @returns {array}
   */
  getBars() {
    return Array.from(this._bars, ({ posX: e, freq: i, freqLo: a, freqHi: n, hold: r, peak: s, value: o }) => ({ posX: e, freq: i, freqLo: a, freqHi: n, hold: r, peak: s, value: o }));
  }
  /**
   * Returns the energy of a frequency, or average energy of a range of frequencies
   *
   * @param [{number|string}] single or initial frequency (Hz), or preset name; if undefined, returns the overall energy
   * @param [{number}] ending frequency (Hz)
   * @returns {number|null} energy value (0 to 1) or null, if the specified preset is unknown
   */
  getEnergy(e, i) {
    if (e === void 0)
      return this._energy.val;
    if (e != +e) {
      if (e == "peak")
        return this._energy.peak;
      const o = {
        bass: [20, 250],
        lowMid: [250, 500],
        mid: [500, 2e3],
        highMid: [2e3, 4e3],
        treble: [4e3, 16e3]
      };
      if (!o[e])
        return null;
      [e, i] = o[e];
    }
    const a = this._freqToBin(e), n = i ? this._freqToBin(i) : a, r = this._chLayout == Re ? 1 : 2;
    let s = 0;
    for (let o = 0; o < r; o++)
      for (let d = a; d <= n; d++)
        s += this._normalizedB(this._fftData[o][d]);
    return s / (n - a + 1) / r;
  }
  /**
   * Returns current analyzer settings in object format
   *
   * @param [{string|array}] a property name or an array of property names to not include in the returned object
   * @returns {object} Options object
   */
  getOptions(e) {
    Array.isArray(e) || (e = [e]);
    let i = {};
    for (const a of Object.keys(tt))
      e.includes(a) || (a == "gradient" && this.gradientLeft != this.gradientRight ? (i.gradientLeft = this.gradientLeft, i.gradientRight = this.gradientRight) : a != "start" && (i[a] = this[a]));
    return i;
  }
  /**
   * Registers a custom gradient
   *
   * @param {string} name
   * @param {object} options
   */
  registerGradient(e, i) {
    if (typeof e != "string" || e.trim().length == 0)
      throw new me(Za);
    if (typeof i != "object")
      throw new me(en);
    const { colorStops: a } = i;
    if (!Array.isArray(a) || !a.length)
      throw new me(tn);
    const n = a.length, r = (s) => +s != s || s < 0 || s > 1;
    a.forEach((s, o) => {
      const d = o / Math.max(1, n - 1);
      typeof s != "object" ? a[o] = { pos: d, color: s } : r(s.pos) && (s.pos = d), r(s.level) && (a[o].level = 1 - o / n);
    }), a.sort((s, o) => s.level < o.level ? 1 : s.level > o.level ? -1 : 0), a[0].level = 1, this._gradients[e] = {
      bgColor: i.bgColor || qa,
      dir: i.dir,
      colorStops: a
    }, this._selectedGrads.includes(e) && this._makeGrad();
  }
  /**
   * Set dimensions of analyzer's canvas
   *
   * @param {number} w width in pixels
   * @param {number} h height in pixels
   */
  setCanvasSize(e, i) {
    this._width = e, this._height = i, this._setCanvas(Nt);
  }
  /**
   * Set desired frequency range
   *
   * @param {number} min lowest frequency represented in the x-axis
   * @param {number} max highest frequency represented in the x-axis
   */
  setFreqRange(e, i) {
    if (e < 1 || i < 1)
      throw new me(qt);
    this._minFreq = Math.min(e, i), this.maxFreq = Math.max(e, i);
  }
  /**
   * Set custom parameters for LED effect
   * If called with no arguments or if any property is invalid, clears any previous custom parameters
   *
   * @param {object} [params]
   */
  setLedParams(e) {
    let i, a, n;
    e && (i = e.maxLeds | 0, // ensure integer
    a = +e.spaceV, n = +e.spaceH), this._ledParams = i > 0 && a > 0 && n >= 0 ? [i, a, n] : void 0, this._calcBars();
  }
  /**
   * Shorthand function for setting several options at once
   *
   * @param {object} options
   */
  setOptions(e) {
    this._setProps(e);
  }
  /**
   * Adjust the analyzer's sensitivity
   *
   * @param {number} min minimum decibels value
   * @param {number} max maximum decibels value
   */
  setSensitivity(e, i) {
    for (const a of [0, 1])
      this._analyzer[a].minDecibels = Math.min(e, i), this._analyzer[a].maxDecibels = Math.max(e, i);
  }
  /**
   * Start the analyzer
   */
  start() {
    this.toggleAnalyzer(!0);
  }
  /**
   * Stop the analyzer
   */
  stop() {
    this.toggleAnalyzer(!1);
  }
  /**
   * Start / stop canvas animation
   *
   * @param {boolean} [force] if undefined, inverts the current state
   * @returns {boolean} resulting state after the change
   */
  toggleAnalyzer(e) {
    const i = this.isOn;
    return e === void 0 && (e = !i), i && !e ? (cancelAnimationFrame(this._runId), this._runId = 0) : !i && e && !this._destroyed && (this._frames = 0, this._time = performance.now(), this._runId = requestAnimationFrame((a) => this._draw(a))), this.isOn;
  }
  /**
   * Toggles canvas full-screen mode
   */
  toggleFullscreen() {
    if (this.isFullscreen)
      document.exitFullscreen ? document.exitFullscreen() : document.webkitExitFullscreen && document.webkitExitFullscreen();
    else {
      const e = this._fsEl;
      if (!e)
        return;
      e.requestFullscreen ? e.requestFullscreen() : e.webkitRequestFullscreen && e.webkitRequestFullscreen();
    }
  }
  /**
   * ==========================================================================
   *
   * PRIVATE METHODS
   *
   * ==========================================================================
   */
  /**
   * Return the frequency (in Hz) for a given FFT bin
   */
  _binToFreq(e) {
    return e * this.audioCtx.sampleRate / this.fftSize || 1;
  }
  /**
   * Compute all internal data required for the analyzer, based on its current settings
   */
  _calcBars() {
    const e = this._bars = [];
    if (!this._ready) {
      this._flg = { isAlpha: !1, isBands: !1, isLeds: !1, isLumi: !1, isOctaves: !1, isOutline: !1, isRound: !1, noLedGap: !1 };
      return;
    }
    const { _ansiBands: i, _barSpace: a, canvas: n, _chLayout: r, _maxFreq: s, _minFreq: o, _mirror: d, _mode: u, _radial: f, _radialInvert: g, _reflexRatio: c } = this, p = n.width >> 1, x = n.height >> 1, M = r == Ae && !f, T = r == dt, l = u % 10 != 0, k = l && this._frequencyScale == Ye, y = this._showLeds && l && !f, v = this._lumiBars && l && !f, q = this._alphaBars && !v && u != $e, N = this._outlineBars && l && !v && !y, I = this._roundBars && l && !v && !y, H = r != Ae || c > 0 && !v, G = n.height - (M && !y ? 0.5 : 0) >> M, F = G * (v || f ? 1 : 1 - c) | 0, h = n.width - p * (T || d != 0), re = M ? n.height - G * 2 : 0, _e = p * (d == -1 && !T && !f);
    let de = Math.min(n.width, n.height) * 0.375 * (r == Ae ? 1 : this._radius) | 0, U = Math.min(p, x);
    g && r != Ae && ([de, U] = [U, de]);
    const W = (R) => e.push({ ...R, peak: [0, 0], hold: [0], alpha: [0], value: [0] }), z = (R) => {
      const B = this._freqToBin(R, "floor"), L = this._binToFreq(B), C = this._binToFreq(B + 1), j = Math.log2(R / L) / Math.log2(C / L);
      return [B, j];
    };
    let P, Z, E;
    if (k) {
      const R = ($, Q, X) => +$.toPrecision(X ? Math.max(Q, 1 + Math.log10($) | 0) : Q), B = ($) => {
        const Q = [1, 1.12, 1.25, 1.4, 1.6, 1.8, 2, 2.24, 2.5, 2.8, 3.15, 3.55, 4, 4.5, 5, 5.6, 6.3, 7.1, 8, 9, 10], X = Math.log10($) | 0, xe = $ / 10 ** X;
        let be = 1;
        for (; be < Q.length && xe > Q[be]; )
          be++;
        return xe - Q[be - 1] < Q[be] - xe && be--, (Q[be] * 10 ** (X + 5) | 0) / 1e5;
      }, L = [0, 24, 12, 8, 6, 4, 3, 2, 1][u], C = i ? 10 ** (3 / (L * 10)) : 2 ** (1 / L), j = C ** 0.5;
      let ne = i ? 7.94328235 / (L % 2 ? 1 : j) : vi;
      do {
        let $ = ne;
        const Q = R($ / j, 4, !0), X = R($ * j, 4, !0), [xe, be] = z(Q), [Ke, ye] = z(X);
        i ? $ = L < 4 ? B($) : R($, $.toString()[0] < 5 ? 3 : 2) : $ = R($, 4, !0), $ >= o && W({ posX: 0, freq: $, freqLo: Q, freqHi: X, binLo: xe, binHi: Ke, ratioLo: be, ratioHi: ye }), ne *= C;
      } while (ne <= s);
      P = h / e.length, e.forEach(($, Q) => $.posX = _e + Q * P);
      const K = e[0], ce = e[e.length - 1];
      Z = this._freqScaling(K.freqLo), E = h / (this._freqScaling(ce.freqHi) - Z), K.freqLo < o && (K.freqLo = o, [K.binLo, K.ratioLo] = z(o)), ce.freqHi > s && (ce.freqHi = s, [ce.binHi, ce.ratioHi] = z(s));
    } else if (l) {
      const R = [0, 24, 12, 8, 6, 4, 3, 2, 1][u] * 10, B = (L) => {
        switch (this._frequencyScale) {
          case Ft:
            return 1960 / (26.81 / (L + 0.53) - 1);
          case Ot:
            return 700 * (2 ** L - 1);
          case et:
            return L;
        }
      };
      P = h / R, Z = this._freqScaling(o), E = h / (this._freqScaling(s) - Z);
      for (let L = 0, C = 0; L < R; L++, C += P) {
        const j = B(Z + C / E), ne = B(Z + (C + P / 2) / E), K = B(Z + (C + P) / E), [ce, $] = z(j), [Q, X] = z(K);
        W({ posX: _e + C, freq: ne, freqLo: j, freqHi: K, binLo: ce, binHi: Q, ratioLo: $, ratioHi: X });
      }
    } else {
      P = 1, Z = this._freqScaling(o), E = h / (this._freqScaling(s) - Z);
      const R = this._freqToBin(o, "floor"), B = this._freqToBin(s);
      let L = -999;
      for (let C = R; C <= B; C++) {
        const j = this._binToFreq(C), ne = _e + Math.round(E * (this._freqScaling(j) - Z));
        if (ne > L)
          W({ posX: ne, freq: j, freqLo: j, freqHi: j, binLo: C, binHi: C, ratioLo: 0, ratioHi: 0 }), L = ne;
        else if (e.length) {
          const K = e[e.length - 1];
          K.binHi = C, K.freqHi = j, K.freq = (K.freqLo * j) ** 0.5;
        }
      }
    }
    let oe = 0, ee = 0;
    if (y) {
      const R = this._pixelRatio / (window.devicePixelRatio > 1 && window.screen.height <= 540 ? 2 : 1), B = [
        [],
        [128, 3, 0.45],
        // mode 1
        [128, 4, 0.225],
        // mode 2
        [96, 6, 0.225],
        // mode 3
        [80, 6, 0.225],
        // mode 4
        [80, 6, 0.125],
        // mode 5
        [64, 6, 0.125],
        // mode 6
        [48, 8, 0.125],
        // mode 7
        [24, 16, 0.125]
        // mode 8
      ], L = this._ledParams, [C, j, ne] = L || B[u];
      let K, ce = F;
      if (L) {
        const $ = 2 * R;
        let Q;
        K = C + 1;
        do
          K--, Q = ce / K / (1 + j), ee = Q * j;
        while ((Q < $ || ee < $) && K > 1);
      } else {
        const $ = 540 / j;
        ee = Math.min(j * R, Math.max(2, ce / $ + 0.1 | 0));
      }
      H && (ce += ee), L || (K = Math.min(C, ce / (ee * 2) | 0)), oe = ne >= 1 ? ne : P * ne, this._leds = [
        K,
        oe,
        ee,
        ce / K - ee
        // ledHeight
      ];
    }
    const _ = Math.min(P - 1, a * (a > 0 && a < 1 ? P : 1));
    l && (P -= Math.max(y ? oe : 0, _)), e.forEach((R, B) => {
      let L = R.posX, C = P;
      l && (a == 0 && !y ? (L |= 0, C |= 0, B > 0 && L > e[B - 1].posX + e[B - 1].width && (L--, C++)) : L += Math.max(y ? oe : 0, _) / 2, R.posX = L), R.barCenter = L + (P == 1 ? 0 : C / 2), R.width = C;
    });
    const w = [];
    for (const R of [0, 1]) {
      const B = r == Ae ? (G + re) * R : 0, L = B + G, C = B + F - (!y || H ? 0 : ee);
      w.push({ channelTop: B, channelBottom: L, analyzerBottom: C });
    }
    this._aux = { analyzerHeight: F, analyzerWidth: h, centerX: p, centerY: x, channelCoords: w, channelHeight: G, channelGap: re, initialX: _e, innerRadius: de, outerRadius: U, scaleMin: Z, unitWidth: E }, this._flg = { isAlpha: q, isBands: l, isLeds: y, isLumi: v, isOctaves: k, isOutline: N, isRound: I, noLedGap: H }, this._createScales();
  }
  /**
   * Generate the X-axis and radial scales in auxiliary canvases
   */
  _createScales() {
    if (!this._ready)
      return;
    const { analyzerWidth: e, initialX: i, innerRadius: a, scaleMin: n, unitWidth: r } = this._aux, { canvas: s, _frequencyScale: o, _mirror: d, _noteLabels: u, _radial: f, _scaleX: g, _scaleR: c } = this, p = g.canvas, x = c.canvas, M = [], T = this._chLayout == dt, l = this._chLayout == Ae, k = Math.min(s.width, s.height), y = ["C", , "D", , "E", "F", , "G", , "A", , "B"], v = k / 34 | 0, q = p.height >> 1, N = v >> 1, I = q * (u ? 0.7 : 1.5), H = N * (u ? 1 : 2), G = 2 ** (1 / 12);
    if (!u && (this._ansiBands || o != Ye))
      M.push(16, 31.5, 63, 125, 250, 500, 1e3, 2e3, 4e3), o == et ? M.push(6e3, 8e3, 1e4, 12e3, 14e3, 16e3, 18e3, 2e4, 22e3) : M.push(8e3, 16e3);
    else {
      let U = vi;
      for (let W = -1; W < 11; W++)
        for (let z = 0; z < 12; z++) {
          if (U >= this._minFreq && U <= this._maxFreq) {
            const P = y[z], Z = P == "C";
            (P && u && !d && !T || Z) && M.push(u ? [U, P + (Z ? W : "")] : U);
          }
          U *= G;
        }
    }
    x.width = x.height = Math.max(k * 0.15, (a << 1) + l * v);
    const F = x.width >> 1, h = F - v * 0.7, re = (U, W) => {
      const z = We * (U / s.width), P = z - Pt, Z = h * Math.cos(P), E = h * Math.sin(P);
      c.save(), c.translate(F + Z, F + E), c.rotate(z), c.fillText(W, 0, 0), c.restore();
    };
    p.width |= 0, g.fillStyle = c.strokeStyle = Xa, g.fillRect(0, 0, p.width, p.height), c.arc(F, F, F - v / 2, 0, We), c.lineWidth = v, c.stroke(), g.fillStyle = c.fillStyle = Mi, g.font = `${q}px ${ht}`, c.font = `${N}px ${ht}`, g.textAlign = c.textAlign = "center";
    let _e = -I / 4, de = -H;
    for (const U of M) {
      const [W, z] = Array.isArray(U) ? U : [U, U < 1e3 ? U | 0 : `${(U / 100 | 0) / 10}k`], P = r * (this._freqScaling(W) - n), Z = p.height * 0.75, E = z[0] == "C", oe = q * (u && !d && !T ? E ? 1.2 : 0.6 : 3);
      if (g.fillStyle = c.fillStyle = E && !d && !T ? Ua : Mi, u) {
        const ee = o == Ye, _ = o == et;
        let w = ["C"];
        if ((ee || W > 2e3 || !_ && W > 250 || (!f || l) && (!_ && W > 125 || W > 1e3)) && w.push("G"), (ee || W > 4e3 || !_ && W > 500 || (!f || l) && (!_ && W > 250 || W > 2e3)) && w.push("E"), (_ && W > 4e3 || (!f || l) && (ee || W > 2e3 || !_ && W > 500)) && w.push("D", "F", "A", "B"), !w.includes(z[0]))
          continue;
      }
      P >= _e + I / 2 && P <= e && (g.fillText(z, T && d == -1 ? e - P : i + P, Z, oe), (T || d && (P > I || d == 1)) && g.fillText(z, T && d != 1 ? e + P : (i || s.width) - P, Z, oe), _e = P + Math.min(oe, g.measureText(z).width) / 2), P >= de + H && P < e - H && (re(T && d == 1 ? e - P : P, z), (T || d && (P > H || d == 1)) && re(T && d != -1 ? e + P : -P, z), de = P);
    }
  }
  /**
   * Redraw the canvas
   * this is called 60 times per second by requestAnimationFrame()
   */
  _draw(e) {
    this._runId = requestAnimationFrame((b) => this._draw(b));
    const i = e - this._time, a = e - this._last, n = this._maxFPS ? 975 / this._maxFPS : 0;
    if (a < n)
      return;
    this._last = e - (n ? a % n : 0), this._frames++, i >= 1e3 && (this._fps = this._frames / i * 1e3, this._frames = 0, this._time = e);
    const {
      isAlpha: r,
      isBands: s,
      isLeds: o,
      isLumi: d,
      isOctaves: u,
      isOutline: f,
      isRound: g,
      noLedGap: c
    } = this._flg, {
      analyzerHeight: p,
      centerX: x,
      centerY: M,
      channelCoords: T,
      channelHeight: l,
      channelGap: k,
      initialX: y,
      innerRadius: v,
      outerRadius: q
    } = this._aux, {
      _bars: N,
      canvas: I,
      _canvasGradients: H,
      _chLayout: G,
      _colorMode: F,
      _ctx: h,
      _energy: re,
      _fadePeaks: _e,
      fillAlpha: de,
      _fps: U,
      _linearAmplitude: W,
      _lineWidth: z,
      maxDecibels: P,
      minDecibels: Z,
      _mirror: E,
      _mode: oe,
      overlay: ee,
      _radial: _,
      showBgColor: w,
      showPeaks: R,
      useCanvas: B,
      _weightingFilter: L
    } = this, C = this._scaleX.canvas, j = this._scaleR.canvas, ne = U * this._peakFadeTime / 1e3, K = U ** 2, ce = this._gravity * 1e3, $ = U * this._peakHoldTime / 1e3, Q = G == xi, X = G == dt, xe = G == Ae, be = G == Re, Ke = o && this._trueLeds && F == mt, ye = _ ? I.width : this._aux.analyzerWidth, Lt = y + ye, Qe = R && this._peakLine && oe == $e, Se = _ ? q - v : p, ha = Se / this._pixelRatio, [ua, yn, Je, Rt] = this._leds || [];
    re.val > 0 && U > 0 && (this._spinAngle += this._spinSpeed * We / 60 / U);
    const fa = (b) => {
      if (this._reflexRatio > 0 && !d && !_) {
        let O, ke;
        this.reflexFit || xe ? (O = xe && b == 0 ? l + k : 0, ke = l - p) : (O = I.height - p * 2, ke = p), h.save(), h.globalAlpha = this.reflexAlpha, this.reflexBright != 1 && (h.filter = `brightness(${this.reflexBright})`), h.setTransform(1, 0, 0, -1, 0, I.height), h.drawImage(I, 0, T[b].channelTop, I.width, p, 0, O, I.width, ke), h.restore();
      }
    }, _a = () => {
      this.showScaleX && (_ ? (h.save(), h.translate(x, M), this._spinSpeed && h.rotate(this._spinAngle + Pt), h.drawImage(j, -j.width >> 1, -j.width >> 1), h.restore()) : h.drawImage(C, 0, I.height - C.height));
    }, pa = (b) => {
      const O = b ** 2, ke = 424.36, le = 11599.29, rt = 25122.25, Te = 544496.41, Le = 148693636, Ne = (Fe) => 20 * Math.log10(Fe);
      switch (L) {
        case Li:
          const Fe = Le * O ** 2 / ((O + ke) * Math.sqrt((O + le) * (O + Te)) * (O + Le));
          return 2 + Ne(Fe);
        case Ri:
          const Oe = Le * O * b / ((O + ke) * Math.sqrt(O + rt) * (O + Le));
          return 0.17 + Ne(Oe);
        case Ci:
          const St = Le * O / ((O + ke) * (O + Le));
          return 0.06 + Ne(St);
        case Ei:
          const st = ((103791848e-2 - O) ** 2 + 108076816e-2 * O) / ((9837328 - O) ** 2 + 11723776 * O), Tt = b / 68966888496476e-18 * Math.sqrt(st / ((O + 79919.29) * (O + 1345600)));
          return Ne(Tt);
        case Si:
          const ot = -4737338981378384e-39 * b ** 6 + 2043828333606125e-30 * b ** 4 - 1363894795463638e-22 * O + 1, qe = 1306612257412824e-34 * b ** 5 - 2118150887518656e-26 * b ** 3 + 5559488023498642e-19 * b, pe = 1246332637532143e-19 * b / Math.hypot(ot, qe);
          return 18.2 + Ne(pe);
      }
      return 0;
    }, Ct = (b, O, ke) => {
      h.beginPath(), h.moveTo(b, O), h.lineTo(b, ke), h.stroke();
    }, Et = (b) => {
      if (b && z) {
        const O = h.globalAlpha;
        h.globalAlpha = 1, h.stroke(), h.globalAlpha = O;
      }
    }, Pe = (b) => Math.max(0, (b * ua | 0) * (Rt + Je) - Je), ga = (b) => {
      re.val = b, re.peak > 0 && (re.hold--, re.hold < 0 && (re.peak += re.hold * ce / K / I.height * this._pixelRatio)), b >= re.peak && (re.peak = b, re.hold = $);
    };
    ee && h.clearRect(0, 0, I.width, I.height);
    let ei = 0;
    const ti = N.length, ii = be ? 1 : 2;
    for (let b = 0; b < ii; b++) {
      const { channelTop: O, channelBottom: ke, analyzerBottom: le } = T[b], rt = this._gradients[this._selectedGrads[b]], Te = rt.colorStops, Le = Te.length, Ne = !w || o && !ee ? "#000" : rt.bgColor, Fe = xe && _ && b ? -1 : 1, Oe = !b && E == -1 || b && E == 1, St = !X || b && E != 1 ? 0 : ye >> (b || !Oe), st = X && Oe ? -1 : 1, Tt = () => {
        const A = C.height, S = A >> 1, D = W ? 100 : P, J = W ? 0 : Z, ie = W ? 20 : 5, fe = p / (D - J), we = E != -1 && (!X || b == 0 || E == 1), Be = E != 1 && (!X || b != E);
        h.save(), h.fillStyle = Di, h.font = `${S}px ${ht}`, h.textAlign = "right", h.lineWidth = 1;
        for (let Me = D; Me > J; Me -= ie) {
          const Xe = O + (D - Me) * fe, se = Me % 2 == 0 | 0;
          if (se) {
            const he = Xe + S * (Xe == O ? 0.8 : 0.35);
            we && h.fillText(Me, A * 0.85, he), Be && h.fillText(Me, (X ? ye : I.width) - A * 0.1, he), h.strokeStyle = Di, h.setLineDash([2, 4]), h.lineDashOffset = 0;
          } else
            h.strokeStyle = Wa, h.setLineDash([2, 8]), h.lineDashOffset = 1;
          h.beginPath(), h.moveTo(y + A * se * we, ~~Xe + 0.5), h.lineTo(Lt - A * se * Be, ~~Xe + 0.5), h.stroke();
        }
        h.restore();
      }, ot = (A, S) => {
        const D = ve[A] + (A < ve.length - 1 ? (ve[A + 1] - ve[A]) * S : 0);
        return isNaN(D) ? -1 / 0 : D;
      }, qe = (A, S = st) => S * We * ((A + St) / I.width) + this._spinAngle, pe = (A, S, D) => {
        const J = v + S * Fe, ie = qe(A, D);
        return [x + J * Math.cos(ie), M + J * Math.sin(ie)];
      }, Bt = (A, S, D, J, ie) => {
        h.beginPath();
        for (const fe of E && !X ? [1, -1] : [st]) {
          const [we, Be] = g ? [qe(A, fe), qe(A + D, fe)] : [];
          h.moveTo(...pe(A, S, fe)), h.lineTo(...pe(A, S + J, fe)), g ? h.arc(x, M, v + (S + J) * Fe, we, Be, fe != 1) : h.lineTo(...pe(A + D, S + J, fe)), h.lineTo(...pe(A + D, S, fe)), g && !ie && h.arc(x, M, v + S * Fe, Be, we, fe == 1);
        }
        Et(ie), h.fill();
      }, Mt = (A = 0, S = 0) => {
        let D;
        if (F == mt && !Ke || oe == $e)
          D = H[b];
        else {
          const J = F == yi ? S % Le : Te.findLastIndex((ie) => o ? Pe(A) <= Pe(ie.level) : A <= ie.level);
          D = Te[J].color;
        }
        h.fillStyle = h.strokeStyle = D;
      };
      if (B) {
        if (X && !_) {
          const A = ye * (b + Oe), S = Oe ? -1 : 1;
          h.setTransform(S, 0, 0, 1, A, 0);
        }
        if ((!ee || w) && (ee && (h.globalAlpha = this.bgAlpha), h.fillStyle = Ne, (b == 0 || !_ && !Q) && h.fillRect(y, O - k, ye, (ee && this.reflexAlpha == 1 ? p : l) + k), h.globalAlpha = 1), this.showScaleY && !d && !_ && (b == 0 || !Q) && Tt(), o ? (h.setLineDash([Rt, Je]), h.lineWidth = N[0].width) : h.lineWidth = f ? Math.min(z, N[0].width / 2) : z, h.save(), !_) {
          const A = new Path2D();
          A.rect(0, O, I.width, p), h.clip(A);
        }
      }
      let ve = this._fftData[b];
      this._analyzer[b].getFloatFrequencyData(ve), L && (ve = ve.map((A, S) => A + pa(this._binToFreq(S)))), h.beginPath();
      let Ze = [];
      for (let A = 0; A < ti; A++) {
        const S = N[A], { posX: D, barCenter: J, width: ie, freq: fe, binLo: we, binHi: Be, ratioLo: Me, ratioHi: Xe } = S;
        let se = Math.max(ot(we, Me), ot(Be, Xe));
        for (let Y = we + 1; Y < Be; Y++)
          ve[Y] > se && (se = ve[Y]);
        if (se = this._normalizedB(se), S.value[b] = se, ei += se, S.peak[b] > 0 && S.alpha[b] > 0 && (S.hold[b]--, S.hold[b] < 0)) {
          if (_e && !Qe) {
            const Y = !r || f && z > 0 ? 1 : r ? S.peak[b] : de;
            S.alpha[b] = Y * (1 + S.hold[b] / ne);
          } else
            S.peak[b] += S.hold[b] * ce / K / Math.abs(ha);
          S.alpha[b] <= 0 && (S.peak[b] = 0);
        }
        if (se >= S.peak[b] && (S.peak[b] = se, S.hold[b] = $, S.alpha[b] = !r || f && z > 0 ? 1 : r ? se : de), !B)
          continue;
        h.globalAlpha = d || r ? se : f ? de : 1, Mt(se, A);
        const he = d ? Se : o ? Pe(se) : se * Se | 0;
        if (oe == $e) {
          const Y = A ? 0 : (this._normalizedB(ve[N[1].binLo]) * Se + he) / 2;
          if (_) {
            if (A == 0 && (X && h.moveTo(...pe(0, 0)), h.lineTo(...pe(0, D < 0 ? Y : he))), D >= 0) {
              const ge = [D, he];
              h.lineTo(...pe(...ge)), Ze.push(ge);
            }
          } else {
            if (A == 0)
              if (E == -1 && !X)
                h.moveTo(y, le - (D < y ? Y : he));
              else {
                const ge = we ? this._normalizedB(ve[we - 1]) * Se : he;
                h.moveTo(y - z, le - ge);
              }
            (X || E != -1 || D >= y) && h.lineTo(D, le - he);
          }
        } else if (o) {
          if (w && !ee && (b == 0 || !Q)) {
            const Y = h.globalAlpha;
            h.strokeStyle = Ha, h.globalAlpha = 1, Ct(J, O, le), h.strokeStyle = h.fillStyle, h.globalAlpha = Y;
          }
          if (Ke) {
            const Y = d ? 0 : Te.findLastIndex((ze) => Pe(se) <= Pe(ze.level));
            let ge = le;
            for (let ze = Le - 1; ze >= Y; ze--) {
              h.strokeStyle = Te[ze].color;
              let ni = le - (ze == Y ? he : Pe(Te[ze].level));
              Ct(J, ge, ni), ge = ni - Je;
            }
          } else
            Ct(J, le, le - he);
        } else if (D >= y)
          if (_)
            Bt(D, 0, ie, he, f);
          else if (g) {
            const Y = ie / 2, ge = le + Y;
            h.beginPath(), h.moveTo(D, ge), h.lineTo(D, ge - he), h.arc(J, ge - he, Y, Zt, We), h.lineTo(D + ie, ge), Et(f), h.fill();
          } else {
            const Y = f ? h.lineWidth : 0;
            h.beginPath(), h.rect(D, le + Y, ie, -he - Y), Et(f), h.fill();
          }
        const Ue = S.peak[b], ai = S.alpha[b];
        if (Ue > 0 && ai > 0 && R && !Qe && !d && D >= y && D < Lt) {
          if (_e ? h.globalAlpha = ai : f && z > 0 ? h.globalAlpha = 1 : r && (h.globalAlpha = Ue), (F == ki || Ke) && Mt(Ue), o) {
            const Y = Pe(Ue);
            Y >= Je && h.fillRect(D, le - Y, ie, Rt);
          } else if (!_)
            h.fillRect(D, le - Ue * Se, ie, 2);
          else if (oe != $e) {
            const Y = Ue * Se;
            Bt(D, Y, ie, !this._radialInvert || xe || Y + v >= 2 ? -2 : 2);
          }
        }
      }
      if (B) {
        if (h.globalAlpha = 1, oe == $e) {
          if (Mt(), _ && !X) {
            if (E) {
              let A;
              for (; A = Ze.pop(); )
                h.lineTo(...pe(...A, -1));
            }
            h.closePath();
          }
          if (z > 0 && h.stroke(), de > 0) {
            if (_) {
              const A = X ? qe(ye >> 1) : 0, S = X ? qe(ye) : We;
              h.moveTo(...pe(X ? ye >> 1 : 0, 0)), h.arc(x, M, v, A, S, X ? !Oe : !0);
            } else
              h.lineTo(Lt, le), h.lineTo(y, le);
            h.globalAlpha = de, h.fill(), h.globalAlpha = 1;
          }
          if ((Qe || _ && R) && (Ze = [], h.beginPath(), N.forEach((A, S) => {
            let D = A.posX, J = A.peak[b], ie = S ? "lineTo" : "moveTo";
            if (_ && D < 0) {
              const fe = N[S + 1];
              J = an(D, J, fe.posX, fe.peak[b], 0), D = 0;
            }
            J *= Se, Qe ? (h[ie](..._ ? pe(D, J) : [D, le - J]), _ && E && !X && Ze.push([D, J])) : A.peak[b] > 0 && (_e && (h.globalAlpha = A.alpha[b]), Bt(D, J, 1, -2));
          }), Qe)) {
            let A;
            for (; A = Ze.pop(); )
              h.lineTo(...pe(...A, -1));
            h.lineWidth = 1, h.stroke();
          }
        }
        h.restore(), X && !_ && h.setTransform(1, 0, 0, 1, 0, 0), (!X && !Q || b) && fa(b);
      }
    }
    if (ga(ei / (ti << ii - 1)), B && (E && !_ && !X && (h.setTransform(-1, 0, 0, 1, I.width - y, 0), h.drawImage(I, y, 0, x, I.height, 0, 0, x, I.height), h.setTransform(1, 0, 0, 1, 0, 0)), h.setLineDash([]), _a()), this.showFPS) {
      const b = C.height;
      h.font = `bold ${b}px ${ht}`, h.fillStyle = za, h.textAlign = "right", h.fillText(Math.round(U), I.width - b, b * 2);
    }
    this.onCanvasDraw && (h.save(), h.fillStyle = h.strokeStyle = H[0], this.onCanvasDraw(this, { timestamp: e, canvasGradients: H }), h.restore());
  }
  /**
   * Return scaled frequency according to the selected scale
   */
  _freqScaling(e) {
    switch (this._frequencyScale) {
      case Ye:
        return Math.log2(e);
      case Ft:
        return 26.81 * e / (1960 + e) - 0.53;
      case Ot:
        return Math.log2(1 + e / 700);
      case et:
        return e;
    }
  }
  /**
   * Return the FFT data bin (array index) which represents a given frequency
   */
  _freqToBin(e, i = "round") {
    const a = this._analyzer[0].frequencyBinCount - 1, n = Math[i](e * this.fftSize / this.audioCtx.sampleRate);
    return n < a ? n : a;
  }
  /**
   * Generate currently selected gradient
   */
  _makeGrad() {
    if (!this._ready)
      return;
    const { canvas: e, _ctx: i, _radial: a, _reflexRatio: n } = this, { analyzerWidth: r, centerX: s, centerY: o, initialX: d, innerRadius: u, outerRadius: f } = this._aux, { isLumi: g } = this._flg, c = this._chLayout == Ae, p = 1 - n, x = g ? e.height : e.height * (1 - n * !c) | 0;
    for (const M of [0, 1]) {
      const T = this._gradients[this._selectedGrads[M]], l = T.colorStops, k = T.dir == "h";
      let y;
      if (a ? y = i.createRadialGradient(s, o, f, s, o, u - (f - u) * c) : y = i.createLinearGradient(...k ? [d, 0, d + r, 0] : [0, 0, 0, x]), l) {
        const v = c && !this._splitGradient && (!k || a);
        for (let q = 0; q < 1 + v; q++) {
          const N = l.length - 1;
          l.forEach((I, H) => {
            let G = I.pos;
            if (v && (G /= 2), c && !g && !a && !k && (G *= p, !v && G > 0.5 * p && (G += 0.5 * n)), q == 1)
              if (a || g) {
                const F = N - H;
                I = l[F], G = 1 - I.pos / 2;
              } else
                H == 0 && G > 0 && y.addColorStop(0.5, I.color), G += 0.5;
            y.addColorStop(G, I.color), c && H == N && G < 0.5 && y.addColorStop(0.5, I.color);
          });
        }
      }
      this._canvasGradients[M] = y;
    }
  }
  /**
   * Normalize a dB value in the [0;1] range
   */
  _normalizedB(e) {
    const i = this._linearAmplitude, a = i ? 1 / this._linearBoost : 1, n = (d, u, f) => d <= u ? u : d >= f ? f : d, r = (d) => 10 ** (d / 20);
    let s = this.maxDecibels, o = this.minDecibels;
    return i && (s = r(s), o = r(o), e = r(e) ** a), n((e - o) / (s - o) ** a, 0, 1);
  }
  /**
   * Internal function to change canvas dimensions on demand
   */
  _setCanvas(e) {
    if (!this._ready)
      return;
    const { canvas: i, _ctx: a } = this, n = this._scaleX.canvas, r = window.devicePixelRatio / (this._loRes + 1);
    let s = window.screen.width * r, o = window.screen.height * r;
    Math.abs(window.orientation) == 90 && s < o && ([s, o] = [o, s]);
    const d = this.isFullscreen, u = d && this._fsEl == i, f = u ? s : (this._width || this._container.clientWidth || this._defaultWidth) * r | 0, g = u ? o : (this._height || this._container.clientHeight || this._defaultHeight) * r | 0;
    this._pixelRatio = r, this._fsWidth = s, this._fsHeight = o, !(e != Ti && i.width == f && i.height == g) && (i.width = f, i.height = g, this.overlay || (a.fillStyle = "#000", a.fillRect(0, 0, f, g)), a.lineJoin = "bevel", n.width = f, n.height = Math.max(20 * r, Math.min(f, g) / 32 | 0), this._calcBars(), this._makeGrad(), this._fsStatus !== void 0 && this._fsStatus !== d && (e = Bi), this._fsStatus = d, this.onCanvasResize && this.onCanvasResize(e, this));
  }
  /**
   * Select a gradient for one or both channels
   *
   * @param {string} name gradient name
   * @param [{number}] desired channel (0 or 1) - if empty or invalid, sets both channels
   */
  _setGradient(e, i) {
    if (!this._gradients.hasOwnProperty(e))
      throw new me(Ya, e);
    [0, 1].includes(i) || (this._selectedGrads[1] = e, i = 0), this._selectedGrads[i] = e, this._makeGrad();
  }
  /**
   * Set object properties
   */
  _setProps(e, i) {
    const a = ["onCanvasDraw", "onCanvasResize"], n = ["gradientLeft", "gradientRight", "stereo"], r = Object.keys(tt).filter((s) => s != "start").concat(a, n);
    (i || e === void 0) && (e = { ...tt, ...e });
    for (const s of Object.keys(e))
      a.includes(s) && typeof e[s] != "function" ? this[s] = void 0 : r.includes(s) && (this[s] = e[s]);
    e.start !== void 0 && this.toggleAnalyzer(e.start);
  }
}
const kt = "#c2ff3a", Fi = /^#[0-9a-f]{6}$/i, oa = (t) => [1, 3, 5].map((e) => parseInt(t.slice(e, e + 2), 16)), rn = (t) => "#" + t.map((e) => Math.max(0, Math.min(255, Math.round(e))).toString(16).padStart(2, "0")).join(""), Ut = (t, e) => rn(oa(t).map((i) => e > 0 ? i + (255 - i) * e : i * (1 + e))), Wt = (t) => [
  Ut(t, 0.3),
  t,
  Ut(t, -0.6)
];
let sn = 0;
const la = () => `r${++sn}`, De = (t) => typeof t == "string" ? t : "";
function Oi(t, e) {
  const i = Array.isArray(t) ? t : t?.records;
  if (!Array.isArray(i)) return [];
  const a = (n) => De(n) ? new URL(De(n), e).href : void 0;
  return i.flatMap((n) => {
    const r = (Array.isArray(n?.tracks) ? n.tracks : []).filter((d) => De(d?.src)).map((d, u) => ({
      title: De(d.title) || `Track ${u + 1}`,
      src: a(d.src),
      duration: typeof d.duration == "number" ? d.duration : void 0
    }));
    if (!r.length) return [];
    const s = Fi.test(De(n.accent)) ? n.accent : kt, o = Array.isArray(n.gradient) && n.gradient.length === 3 && n.gradient.every((d) => Fi.test(De(d)));
    return [
      {
        id: la(),
        title: De(n.title) || "Untitled",
        artist: De(n.artist),
        cover: a(n.cover),
        link: a(n.link),
        accent: s,
        gradient: o ? n.gradient : Wt(s),
        tracks: r
      }
    ];
  });
}
const on = {
  connectSpeakers: !1,
  overlay: !0,
  showBgColor: !1,
  showScaleX: !1,
  showPeaks: !1,
  alphaBars: !0,
  frequencyScale: "log",
  smoothing: 0.72,
  minDecibels: -85,
  maxDecibels: -22,
  start: !1
};
function ca(t, e) {
  const i = new nn(t, { ...on, ...e }), a = (r, s) => {
    i.registerGradient(r, { bgColor: "transparent", colorStops: [...s] }), i.gradient !== r && (i.gradient = r);
  };
  let n = 0;
  return {
    setRecord: (r) => a(r.id, r.gradient),
    setMix(r) {
      const s = r.reduce((d, [, u]) => d + u, 0);
      if (s < 1e-4) return;
      const o = [0, 1, 2].map((d) => {
        const u = [0, 0, 0];
        for (const [f, g] of r) oa(f.gradient[d]).forEach((c, p) => u[p] += c * g / s);
        return `rgb(${u.map(Math.round).join(",")})`;
      });
      a("mix", o);
    },
    setActive(r) {
      window.clearTimeout(n), t.classList.toggle("is-on", r), r ? !i.isOn && i.start() : n = window.setTimeout(() => i.stop(), 1200);
    }
  };
}
const ln = (t, e) => ca(e, { source: t, mode: 3, barSpace: 0.3 }), cn = (t, e) => ca(e, { source: t, mode: 4, barSpace: 0.35, radial: !0, radius: 0.7, spinSpeed: 2 }), qi = 200, zt = -8, it = 4, Ht = 27, zi = { x: 0.98, y: 0.02 }, Hi = (t, e, i, a) => Math.atan2(e - a, t - i) * 180 / Math.PI, dn = (t) => ((t + 180) % 360 + 360) % 360 - 180, hn = (t) => String(t).padStart(2, "0");
function un(t) {
  const { side: e, engine: i, channel: a, dj: n, records: r, t: s } = t, [, o] = He(0), d = () => o((_) => _ + 1), u = ue(null), f = ue(null), g = ue(null), c = ue(null), p = ue(null), x = ue(null), M = ue(""), T = ue(t);
  T.current = t;
  const l = ue({
    rec: null,
    track: 0,
    loaded: "",
    // "rec/track" des Buffers im Worklet
    needle: !1,
    // Nadel auf der Platte = Ton
    loading: !1,
    failed: !1,
    pos: 0,
    rate: 1,
    angle: 0,
    armAngle: zt,
    scratch: null,
    armDrag: !1,
    braking: !1,
    pitch: 0,
    // ±0.08 = ±8 %
    cues: [null, null, null],
    dropping: !1
  }).current, k = () => i.unlock().then(() => {
    !x.current && c.current && (x.current = cn(a.output, c.current));
  }), y = (_, w, R = { sec: 0 }) => {
    const B = `${_.id}/${w}`;
    Object.assign(l, { rec: _, track: w, needle: !0, braking: !1, failed: !1 });
    const L = _.tracks[w].duration;
    l.loaded === B && L ? (l.pos = "sec" in R ? R.sec : R.frac * L, a.seek(l.pos)) : (l.loading = !0, l.loaded = "", l.pos = 0, a.load(_.tracks[w].src, (C) => "sec" in R ? R.sec : R.frac * C).then((C) => {
      C !== null && (_.tracks[w].duration = C, l.loaded = B, l.loading = !1, d());
    }).catch(() => {
      Object.assign(l, { loading: !1, needle: !1, failed: !0 }), d();
    })), k(), d();
  }, v = () => {
    l.needle = !1, l.braking = !1, d();
  }, q = () => {
    if (k(), !l.rec) return r[0] && y(r[0], 0);
    l.needle && !l.braking ? l.braking = !0 : (l.needle || (l.rate = 0), l.needle = !0, l.braking = !1), d();
  }, N = (_) => {
    if (!l.rec) return;
    const w = l.track + _;
    w >= 0 && w < l.rec.tracks.length && y(l.rec, w);
  };
  Ee(() => {
    a.onPosition = (B, L) => {
      l.loading || (l.pos = B, !(!L || !l.needle || !l.rec) && (l.track + 1 < l.rec.tracks.length ? y(l.rec, l.track + 1) : (Object.assign(l, { needle: !1, track: 0, pos: 0 }), a.seek(0), d())));
    };
    let _ = 0, w = performance.now();
    const R = (B) => {
      const L = Math.min(0.05, (B - w) / 1e3);
      if (w = B, l.scratch)
        B - l.scratch.t > 50 && (l.scratch.vel *= 0.6), l.rate = l.scratch.vel;
      else {
        const C = l.braking ? 0 : 1 + l.pitch;
        l.rate += (C - l.rate) * Math.min(1, L * (l.braking ? 2.2 : 5)), Math.abs(C - l.rate) < 2e-3 && (l.rate = C), l.braking && l.rate < 0.02 && v(), l.angle += l.rate * qi * L;
      }
      if (a.setRate(l.needle && !l.loading && !l.armDrag ? l.rate : 0), !l.armDrag) {
        let C = zt;
        if (l.needle && l.rec) {
          const j = l.rec.tracks[l.track].duration, ne = j ? Math.min(1, l.pos / j) : 0;
          C = it + (Ht - it) * (l.track + ne) / l.rec.tracks.length;
        }
        l.armAngle += (C - l.armAngle) * Math.min(1, L * 6);
      }
      f.current && (f.current.style.transform = `rotate(${l.angle}deg)`), g.current && (g.current.style.transform = `rotate(${l.armAngle}deg)`), _ = requestAnimationFrame(R);
    };
    return _ = requestAnimationFrame(R), () => cancelAnimationFrame(_);
  }, []), Ee(() => {
    const _ = l.needle && !l.loading;
    l.rec && M.current !== l.rec.id && (x.current?.setRecord(l.rec), x.current && (M.current = l.rec.id)), x.current?.setActive(_), T.current.onStatus(e, { playing: _ && !l.braking, rec: l.rec });
  }), Ee(() => {
    n || (l.pitch = 0, e === "b" && v());
  }, [n]), Ee(() => {
    l.rec && !r.includes(l.rec) && (Object.assign(l, { rec: null, needle: !1 }), d());
  }, [r]);
  const I = () => {
    const _ = u.current.getBoundingClientRect();
    return { cx: _.left + _.width / 2, cy: _.top + _.height / 2, r: _ };
  }, H = (_) => {
    k(), _.currentTarget.setPointerCapture(_.pointerId);
    const { cx: w, cy: R } = I();
    l.scratch = { last: Hi(_.clientX, _.clientY, w, R), t: performance.now(), vel: 0 };
  }, G = (_) => {
    if (!l.scratch) return;
    const { cx: w, cy: R } = I(), B = Hi(_.clientX, _.clientY, w, R), L = performance.now(), C = dn(B - l.scratch.last), j = Math.max(4e-3, (L - l.scratch.t) / 1e3);
    l.angle += C;
    const ne = Math.max(-4, Math.min(4, C / j / qi));
    l.scratch.vel = l.scratch.vel * 0.5 + ne * 0.5, l.scratch.last = B, l.scratch.t = L;
  }, F = () => {
    l.scratch = null;
  }, h = (_) => {
    const { r: w } = I(), R = w.left + zi.x * w.width, B = w.top + zi.y * w.height, L = Math.atan2(-(_.clientX - R), _.clientY - B) * 180 / Math.PI;
    return Math.max(zt, Math.min(Ht + 2, L));
  }, re = (_) => {
    k(), _.stopPropagation(), _.currentTarget.setPointerCapture(_.pointerId), l.armDrag = !0;
  }, _e = (_) => {
    l.armDrag && (l.armAngle = h(_));
  }, de = (_) => {
    if (!l.armDrag) return;
    l.armDrag = !1;
    const w = h(_), R = l.rec ?? r[0];
    if (w < it - 1.5 || !R) return v();
    const B = Math.min(0.999, Math.max(0, (w - it) / (Ht - it))) * R.tracks.length, L = Math.floor(B);
    y(R, L, { frac: B - L });
  }, U = async (_) => {
    k();
    const w = await T.current.onFiles(_);
    w && y(w, 0);
  }, W = (_) => {
    !t.uploads || !_.dataTransfer?.files.length || (_.preventDefault(), _.stopPropagation(), l.dropping = !1, U([..._.dataTransfer.files]));
  }, z = (_) => {
    !t.uploads || !_.dataTransfer?.types.includes("Files") || (_.preventDefault(), l.dropping || (l.dropping = !0, d()));
  }, P = (_) => {
    k();
    const w = l.cues[_];
    if (w) return y(w.rec, w.track, { sec: w.pos });
    l.rec && (l.cues[_] = { rec: l.rec, track: l.track, pos: l.pos }, d());
  }, Z = (_) => {
    l.cues[_] = null, d();
  }, E = l.rec, oe = l.needle && !l.braking, ee = E && (E.link ? /* @__PURE__ */ m("a", { href: E.link, target: "_blank", rel: "noopener", children: E.artist }) : E.artist);
  return /* @__PURE__ */ m(
    "div",
    {
      class: `at-deck at-deck-${e}${l.dropping ? " is-dropping" : ""}`,
      style: E ? { "--deck-accent": E.accent } : void 0,
      onDragOver: z,
      onDragLeave: () => l.dropping && (l.dropping = !1, d()),
      onDrop: W,
      children: [
        /* @__PURE__ */ m("div", { ref: u, class: "at-deck-stage", children: [
          /* @__PURE__ */ m("div", { ref: c, class: "at-deck-ring", "aria-hidden": "true" }),
          /* @__PURE__ */ m(
            "div",
            {
              class: "at-deck-disc",
              onPointerDown: H,
              onPointerMove: G,
              onPointerUp: F,
              onPointerCancel: F,
              children: /* @__PURE__ */ m("div", { ref: f, class: "at-vinyl", children: E && /* @__PURE__ */ m("div", { class: "at-deck-label", children: E.cover ? /* @__PURE__ */ m("img", { src: E.cover, alt: "", draggable: !1 }) : /* @__PURE__ */ m("span", { children: E.title }) }) })
            },
            E?.id ?? "idle"
          ),
          !E && e === "a" && t.logo && /* @__PURE__ */ m("img", { class: "at-vinyl-logo", src: t.logo, alt: "", draggable: !1 }),
          l.dropping && /* @__PURE__ */ m("div", { class: "at-deck-drop", children: s.drop }),
          /* @__PURE__ */ m(
            "div",
            {
              ref: g,
              class: "at-vinyl-arm",
              onPointerDown: re,
              onPointerMove: _e,
              onPointerUp: de,
              onPointerCancel: de
            }
          )
        ] }),
        /* @__PURE__ */ m("div", { class: "at-deck-ui", children: [
          /* @__PURE__ */ m("div", { class: "at-deck-sleeves", children: [
            r.map((_) => /* @__PURE__ */ m(
              "button",
              {
                class: "at-deck-sleeve",
                style: { "--sleeve-accent": _.accent },
                "aria-pressed": E === _,
                title: `${_.artist} – ${_.title}`,
                onClick: () => y(_, 0),
                children: _.cover ? /* @__PURE__ */ m("img", { src: _.cover, alt: `${_.artist} – ${_.title}` }) : /* @__PURE__ */ m("span", { children: _.title })
              },
              _.id
            )),
            t.uploads && /* @__PURE__ */ m("button", { class: "at-deck-sleeve at-deck-upload", title: s.upload, "aria-label": s.upload, onClick: () => p.current.click(), children: [
              "+",
              /* @__PURE__ */ m(
                "input",
                {
                  ref: p,
                  type: "file",
                  accept: "audio/*",
                  multiple: !0,
                  hidden: !0,
                  onChange: (_) => {
                    const w = _.currentTarget;
                    U([...w.files ?? []]), w.value = "";
                  }
                }
              )
            ] })
          ] }),
          /* @__PURE__ */ m("div", { class: "at-deck-now", children: [
            /* @__PURE__ */ m("div", { class: "at-deck-meta", "aria-live": "polite", children: E ? /* @__PURE__ */ m(Ge, { children: [
              /* @__PURE__ */ m("span", { class: "at-deck-band", children: [
                ee,
                E.artist && " · ",
                E.title
              ] }),
              /* @__PURE__ */ m("span", { class: "at-deck-track", children: [
                hn(l.track + 1),
                " · ",
                E.tracks[l.track].title,
                l.loading && ` · ${s.loading}`,
                l.failed && ` · ${s.error}`
              ] })
            ] }) : /* @__PURE__ */ m(Ge, { children: [
              /* @__PURE__ */ m("span", { class: "at-deck-band", children: n ? `${s.deck} ${e.toUpperCase()}` : s.pick }),
              /* @__PURE__ */ m("span", { class: "at-deck-track", children: n ? s.pick : s.hint })
            ] }) }),
            /* @__PURE__ */ m("div", { class: "at-deck-controls", children: [
              /* @__PURE__ */ m("button", { class: "at-deck-btn", onClick: () => N(-1), "aria-label": s.prev, children: "⏮" }),
              /* @__PURE__ */ m("button", { class: "at-deck-btn at-deck-btn-main", onClick: q, "aria-label": oe ? s.pause : s.play, children: oe ? "❚❚" : "▶" }),
              /* @__PURE__ */ m("button", { class: "at-deck-btn", onClick: () => N(1), "aria-label": s.next, children: "⏭" })
            ] })
          ] }),
          n && /* @__PURE__ */ m("div", { class: "at-deck-dj", children: [
            /* @__PURE__ */ m("label", { class: "at-fader", children: [
              /* @__PURE__ */ m("span", { children: s.pitch }),
              /* @__PURE__ */ m(
                "input",
                {
                  type: "range",
                  min: -0.08,
                  max: 0.08,
                  step: 1e-3,
                  defaultValue: "0",
                  onInput: (_) => l.pitch = +_.currentTarget.value,
                  onDblClick: (_) => {
                    _.currentTarget.value = "0", l.pitch = 0;
                  }
                }
              )
            ] }),
            /* @__PURE__ */ m("div", { class: "at-pads", children: [
              l.cues.map((_, w) => /* @__PURE__ */ m(
                "button",
                {
                  class: "at-pad",
                  "aria-pressed": !!_,
                  title: _ ? s.cueJump : s.cueSet,
                  onClick: () => P(w),
                  onDblClick: () => Z(w),
                  children: [
                    "Cue ",
                    w + 1
                  ]
                },
                w
              )),
              /* @__PURE__ */ m(
                "button",
                {
                  class: "at-pad",
                  onClick: () => {
                    k(), l.scratch || (l.rate = -4);
                  },
                  children: s.spin
                }
              )
            ] })
          ] }),
          t.children
        ] })
      ]
    }
  );
}
const $t = (t, e) => (t[e] & 127) << 21 | (t[e + 1] & 127) << 14 | (t[e + 2] & 127) << 7 | t[e + 3] & 127, Gi = (t, e) => (t[e] << 24 | t[e + 1] << 16 | t[e + 2] << 8 | t[e + 3]) >>> 0;
function da(t) {
  return t.length < 10 || t[0] !== 73 || t[1] !== 68 || t[2] !== 51 ? 0 : 10 + $t(t, 6);
}
const ji = ["latin1", "utf-16", "utf-16be", "utf-8"].map((t) => new TextDecoder(t));
function Ve(t, e, i, a) {
  const n = a === 1 || a === 2;
  let r = e;
  if (n) for (; r + 1 < i && !(t[r] === 0 && t[r + 1] === 0); ) r += 2;
  else for (; r < i && t[r] !== 0; ) r++;
  return [(ji[a] ? ji[a].decode(t.subarray(e, r)) : "").replace(/^﻿/, "").trim(), Math.min(i, r + (n ? 2 : 1))];
}
function fn(t) {
  const e = new Uint8Array(t), i = da(e);
  if (!i) return {};
  const a = e[3];
  if (a !== 3 && a !== 4) return {};
  const n = Math.min(i, e.length);
  let r = 10;
  e[5] & 64 && (r += a === 4 ? $t(e, 10) : Gi(e, 10) + 4);
  const s = {};
  for (; r + 10 <= n; ) {
    const o = String.fromCharCode(e[r], e[r + 1], e[r + 2], e[r + 3]);
    if (!/^[A-Z0-9]{4}$/.test(o)) break;
    const d = a === 4 ? $t(e, r + 4) : Gi(e, r + 4), u = r + 10, f = Math.min(n, u + d);
    if (r = f, d <= 0) continue;
    const g = e[u];
    if (o === "TIT2") s.title = Ve(e, u + 1, f, g)[0];
    else if (o === "TPE1") s.artist = Ve(e, u + 1, f, g)[0];
    else if (o === "TALB") s.album = Ve(e, u + 1, f, g)[0];
    else if (o === "TRCK") s.track = parseInt(Ve(e, u + 1, f, g)[0], 10) || void 0;
    else if (o === "APIC" && !s.picture) {
      const [c, p] = Ve(e, u + 1, f, 0), [, x] = Ve(e, p + 1, f, g);
      s.picture = { mime: c.includes("/") ? c : `image/${c || "jpeg"}`, data: e.slice(x, f) };
    }
  }
  return s;
}
async function _n(t) {
  try {
    const e = da(new Uint8Array(await t.slice(0, 10).arrayBuffer()));
    return e ? fn(await t.slice(0, e).arrayBuffer()) : {};
  } catch {
    return {};
  }
}
function pn(t) {
  const e = t.replace(/\.[^.]+$/, "").replace(/_/g, " "), i = e.match(/^\s*(\d{1,3})\s*[-.)_ ]\s*(.+)$/);
  return i ? [parseInt(i[1], 10), i[2].trim()] : [void 0, e.trim()];
}
async function gn(t) {
  try {
    const e = await createImageBitmap(await (await fetch(t)).blob()), a = new OffscreenCanvas(16, 16).getContext("2d");
    a.drawImage(e, 0, 0, 16, 16);
    const n = a.getImageData(0, 0, 16, 16).data;
    let r = 0, s = 0, o = 0, d = 0;
    for (let f = 0; f < n.length; f += 4) {
      const g = Math.max(n[f], n[f + 1], n[f + 2]), c = Math.min(n[f], n[f + 1], n[f + 2]), p = 1 + (g - c) / 32;
      r += n[f] * p, s += n[f + 1] * p, o += n[f + 2] * p, d += p;
    }
    const u = "#" + [r, s, o].map((f) => Math.round(f / d).toString(16).padStart(2, "0")).join("");
    return Ut(u, 0.35);
  } catch {
    return kt;
  }
}
async function mn(t, e, i) {
  const a = t.filter((o) => o.type.startsWith("audio/") || /\.(mp3|m4a|aac|wav|ogg|oga|opus|flac|webm)$/i.test(o.name)), n = [...e], r = /* @__PURE__ */ new Set(), s = await Promise.all(a.map(async (o) => ({ f: o, tags: await _n(o) })));
  for (const { f: o, tags: d } of s) {
    const [u, f] = pn(o.name), g = d.album || i, c = d.artist || "";
    let p = n.find((M) => M.id.startsWith("u") && M.title === g && M.artist === c);
    p || (p = { id: "u" + la(), title: g, artist: c, accent: kt, gradient: Wt(kt), tracks: [] }, n.push(p)), !p.cover && d.picture && (p.cover = URL.createObjectURL(new Blob([d.picture.data], { type: d.picture.mime })), p.accent = await gn(p.cover), p.gradient = Wt(p.accent));
    const x = { title: d.title || f, src: URL.createObjectURL(o), n: d.track ?? u ?? 999 };
    p.tracks.push(x), p.tracks.sort((M, T) => (M.n ?? 999) - (T.n ?? 999)), r.add(p);
  }
  return { records: n, touched: [...r] };
}
function bn(t) {
  const e = Da(t.lang), [i] = He(() => new Ta()), [a, n] = He(t.decks === 2 && t.mode === "dj"), [r, s] = He(0), [o, d] = He([]), [u, f] = He(!1), g = ue(null), c = ue([]), p = ue(null), x = ue({ a: { playing: !1, rec: null }, b: { playing: !1, rec: null } }), M = [...t.records, ...o];
  Ee(() => () => i.close(), [i]), Ee(() => {
    c.current.forEach((v) => v.inert = !a), t.host.toggleAttribute("data-dj", a), t.host.dispatchEvent(new CustomEvent("modechange", { detail: { mode: a ? "dj" : "single" }, bubbles: !0 }));
  }, [a]), Ee(() => {
    n(t.decks === 2 && t.mode === "dj");
  }, [t.mode, t.decks]);
  const T = () => {
    a ? i.reset() : s((v) => v + 1), n(!a);
  }, l = (v, q) => {
    x.current[v] = q;
    const N = x.current.a.playing || x.current.b.playing;
    !p.current && N && i.output && g.current && (p.current = ln(i.output, g.current)), p.current?.setActive(N);
  };
  Ee(() => {
    const v = { a: 0, b: 0 }, q = window.setInterval(() => {
      const N = x.current;
      if (!p.current || !(N.a.playing || N.b.playing)) return;
      const I = [];
      ["a", "b"].forEach((H, G) => {
        const F = N[H].playing ? i.channels[G].level() : 0;
        v[H] = v[H] * 0.75 + F * 0.25, N[H].rec && I.push([N[H].rec, v[H]]);
      }), p.current.setMix(I);
    }, 80);
    return () => window.clearInterval(q);
  }, [i]);
  const k = async (v) => {
    const { records: q, touched: N } = await mn(v, o, e.myMusic);
    return d(q), N[0];
  }, y = (v, q) => /* @__PURE__ */ m(
    un,
    {
      side: v,
      engine: i,
      channel: i.channels[v === "a" ? 0 : 1],
      dj: a,
      records: M,
      logo: t.logo,
      uploads: t.uploads,
      t: e,
      onStatus: l,
      onFiles: k,
      children: q
    }
  );
  return /* @__PURE__ */ m("div", { class: `at-root${a ? " is-dj" : ""}${u ? " has-hero" : ""}`, children: [
    /* @__PURE__ */ m("div", { ref: g, class: "at-bg-viz", "aria-hidden": "true" }),
    /* @__PURE__ */ m("div", { class: "at-inner", children: [
      /* @__PURE__ */ m("div", { class: "at-hero", children: /* @__PURE__ */ m(
        "slot",
        {
          ref: (v) => {
            if (!v || v._watched) return;
            v._watched = !0;
            const q = () => f(v.assignedNodes().some((N) => N.nodeType === 1 || N.textContent?.trim()));
            v.addEventListener("slotchange", q), q();
          }
        }
      ) }),
      y(
        "a",
        t.decks === 2 && /* @__PURE__ */ m("button", { class: "at-dj-toggle", onClick: T, hidden: a, children: e.open })
      ),
      t.decks === 2 && /* @__PURE__ */ m(Ge, { children: [
        /* @__PURE__ */ m("div", { ref: (v) => void (v && (c.current[0] = v)), class: "at-dj-wrap", children: /* @__PURE__ */ m(Na, { engine: i, active: a, t: e, onClose: T }, r) }),
        /* @__PURE__ */ m("div", { ref: (v) => void (v && (c.current[1] = v)), class: "at-dj-wrap", children: y("b") })
      ] })
    ] })
  ] });
}
const vn = ':host{--at-accent: #c2ff3a;--at-text: #f4f2ec;--at-muted: #8c8a82;--at-font: inherit;--at-font-display: "Anton", Impact, "Arial Narrow", sans-serif;--at-font-mono: "Space Mono", ui-monospace, SFMono-Regular, Menlo, monospace;display:block;position:relative;color:var(--at-text);font-family:var(--at-font)}:host([hidden]){display:none}*{box-sizing:border-box}button,input{font:inherit}.at-root{position:relative;container-type:inline-size;overflow:hidden}.at-inner{display:flex;flex-direction:column;justify-content:center;min-height:var(--at-min-height, 780px);padding:var(--at-padding, 48px 5%)}.at-hero{position:relative;z-index:1}.at-root:not(.has-hero) .at-hero{display:none}@keyframes at-float{0%,to{transform:translateY(0) scale(1)}50%{transform:translateY(-40px) scale(1.06)}}.at-bg-viz,.at-deck-ring{position:absolute;pointer-events:none;opacity:0;transition:opacity 1.1s ease;mix-blend-mode:screen}.at-bg-viz{inset:auto 0 0;height:78%;-webkit-mask-image:linear-gradient(to top,#000 20%,rgba(0,0,0,.35) 60%,transparent);mask-image:linear-gradient(to top,#000 20%,rgba(0,0,0,.35) 60%,transparent)}.at-bg-viz.is-on{opacity:.42}.at-deck-ring{inset:-22%;filter:blur(.5px)}.at-deck-ring.is-on{opacity:.85}.at-bg-viz canvas,.at-deck-ring canvas{display:block}.at-root{--dj-ease: cubic-bezier(.65, 0, .2, 1);--w-single: clamp(260px, min(34cqw, 100vh - 370px), 540px);--dj-w: clamp(220px, min((90cqw - 380px) / 2, 100vh - 470px), 480px)}.at-deck{--deck-accent: var(--at-accent);position:absolute;top:50%;right:5cqw;z-index:1;width:var(--w-single);margin-top:30px;transform:translateY(-50%);transition:right .9s var(--dj-ease),width .9s var(--dj-ease),opacity .7s ease,transform .9s var(--dj-ease)}.at-deck-stage{position:relative;aspect-ratio:1}.at-deck-disc{position:absolute;inset:0;border-radius:50%;cursor:grab;touch-action:pan-y;animation:at-deck-in .7s cubic-bezier(.16,1,.3,1)}.at-deck-disc:active{cursor:grabbing}@keyframes at-deck-in{0%{opacity:0;transform:translate(-35%) rotate(-60deg) scale(.9)}}.at-vinyl{position:absolute;inset:0;border-radius:50%;background:conic-gradient(from 30deg,transparent 0 8%,rgba(255,255,255,.09) 12%,transparent 18% 58%,rgba(255,255,255,.06) 62%,transparent 68%),repeating-radial-gradient(circle,#111 0 2px,#1b1b1d 3px 4px),#111;box-shadow:0 0 0 1px #ffffff0f,0 40px 120px #000000b3,0 0 90px color-mix(in srgb,var(--deck-accent) 16%,transparent);transition:box-shadow .6s}.at-vinyl:after{content:"";position:absolute;top:50%;left:50%;width:2.4%;height:2.4%;margin:-1.2% 0 0 -1.2%;border-radius:50%;background:#d8d6ce;box-shadow:0 0 0 3px #0a0a0b}.at-deck-label{position:absolute;inset:31%;width:38%;height:38%;border-radius:50%;object-fit:cover;box-shadow:0 0 0 4px #0a0a0b,0 0 0 6px color-mix(in srgb,var(--deck-accent) 60%,transparent);pointer-events:none;user-select:none}.at-vinyl-logo{position:absolute;inset:17%;width:66%;height:66%;filter:drop-shadow(0 18px 40px rgba(0,0,0,.75));animation:at-float 9s ease-in-out infinite;pointer-events:none}.at-vinyl-arm{position:absolute;top:2%;left:98%;width:30px;height:62%;margin-left:-15px;background:linear-gradient(#d8d6ce,#7c7a72) center / 6px 100% no-repeat;transform-origin:50% 0;transform:rotate(-8deg);filter:drop-shadow(0 10px 18px rgba(0,0,0,.6));cursor:grab;touch-action:none}.at-vinyl-arm:active{cursor:grabbing}.at-vinyl-arm:before{content:"";position:absolute;top:-14px;left:1px;width:28px;height:28px;border-radius:50%;background:#2a2a2c;border:2px solid #7c7a72;box-sizing:border-box}.at-vinyl-arm:after{content:"";position:absolute;bottom:-8px;left:7px;width:16px;height:22px;border-radius:3px;background:var(--deck-accent);transition:background .6s}.at-deck-ui{display:flex;flex-direction:column;gap:16px;margin-top:28px}.at-deck-sleeves{display:flex;justify-content:center;gap:14px}.at-deck-sleeve{width:64px;height:64px;padding:0;border:2px solid transparent;border-radius:4px;overflow:hidden;background:none;cursor:pointer;opacity:.55;transition:opacity .3s,transform .3s,border-color .3s,box-shadow .3s}.at-deck-sleeve img{display:block;width:100%;height:100%}.at-deck-sleeve:hover{opacity:.9;transform:translateY(-3px)}.at-deck-sleeve[aria-pressed=true]{opacity:1;border-color:var(--deck-accent);transform:translateY(-5px);box-shadow:0 12px 30px color-mix(in srgb,var(--deck-accent) 35%,transparent)}.at-deck-now{display:flex;align-items:center;justify-content:space-between;gap:16px;padding-top:14px;border-top:1px solid rgba(255,255,255,.08)}.at-deck-meta{display:flex;flex-direction:column;gap:4px;min-width:0}.at-deck-band{font-family:var(--at-font-mono);font-size:11px;letter-spacing:.2em;text-transform:uppercase;color:var(--deck-accent)}.at-deck-track{font-size:15px;color:var(--at-text);white-space:nowrap;overflow:hidden;text-overflow:ellipsis}.at-deck-controls{display:flex;flex:none;gap:8px}.at-deck-btn{width:40px;height:40px;border:1px solid rgba(255,255,255,.22);border-radius:4px;background:transparent;color:var(--at-text);font-size:13px;cursor:pointer;transition:border-color .25s,color .25s,transform .25s}.at-deck-btn:hover{border-color:var(--deck-accent);color:var(--deck-accent)}.at-deck-btn-main,.at-deck-btn-main:hover{border-color:transparent;background:var(--deck-accent);color:#0a0a0b}.at-deck-btn-main:hover{transform:translateY(-2px)}.at-deck-dj{display:flex;flex-direction:column;gap:12px}.at-fader{display:flex;flex-direction:column;gap:6px;font-family:var(--at-font-mono);font-size:10px;letter-spacing:.16em;text-transform:uppercase;color:var(--at-muted);text-align:center}.at-fader input{width:100%;height:22px;margin:0;background:transparent;cursor:pointer;-webkit-appearance:none;appearance:none}.at-fader input::-webkit-slider-runnable-track{height:3px;border-radius:2px;background:linear-gradient(90deg,transparent calc(50% - 1px),rgba(255,255,255,.5) calc(50% - 1px) calc(50% + 1px),transparent calc(50% + 1px)),#ffffff24}.at-fader input::-moz-range-track{height:3px;border-radius:2px;background:#ffffff24}.at-fader input::-webkit-slider-thumb{width:14px;height:22px;margin-top:-9.5px;border:0;border-radius:3px;background:linear-gradient(var(--at-text) 0 44%,var(--deck-accent) 44% 56%,#bdbbb2 56%);box-shadow:0 4px 10px #0009;-webkit-appearance:none;appearance:none}.at-fader input::-moz-range-thumb{width:14px;height:22px;border:0;border-radius:3px;background:linear-gradient(var(--at-text) 0 44%,var(--deck-accent) 44% 56%,#bdbbb2 56%);box-shadow:0 4px 10px #0009}.at-fader input:focus-visible{outline:2px solid var(--deck-accent);outline-offset:4px}.at-pads{display:grid;grid-auto-flow:column;grid-auto-columns:1fr;gap:8px}.at-pad[aria-pressed=true]{border-color:var(--deck-accent);color:var(--deck-accent);box-shadow:inset 0 0 12px color-mix(in srgb,var(--deck-accent) 30%,transparent)}.at-pad{padding:10px 0;border:1px solid color-mix(in srgb,var(--deck-accent) 45%,transparent);border-radius:4px;background:color-mix(in srgb,var(--deck-accent) 7%,transparent);color:var(--at-text);font-family:var(--at-font-mono);font-size:11px;letter-spacing:.18em;text-transform:uppercase;cursor:pointer;user-select:none;touch-action:manipulation;transition:background .15s,box-shadow .15s,transform .1s}.at-pad:hover{background:color-mix(in srgb,var(--deck-accent) 16%,transparent)}.at-pad:active{background:var(--deck-accent);color:#0a0a0b;box-shadow:0 0 24px color-mix(in srgb,var(--deck-accent) 55%,transparent);transform:scale(.97)}.at-dj-wrap{display:contents}.at-dj-toggle{width:100%;padding:11px 0;border:1px solid rgba(255,255,255,.18);border-radius:4px;background:transparent;color:#bdbbb2;font-family:var(--at-font-mono);font-size:11px;letter-spacing:.22em;text-transform:uppercase;cursor:pointer;transition:color .25s,border-color .25s,box-shadow .25s}.at-dj-toggle:hover{color:var(--deck-accent, var(--at-accent));border-color:var(--deck-accent, var(--at-accent));box-shadow:0 0 20px color-mix(in srgb,var(--deck-accent, var(--at-accent)) 25%,transparent)}.at-hero{transition:opacity .6s ease,transform .9s var(--dj-ease)}.at-root.is-dj .at-hero{opacity:0;transform:translate(-12cqw);pointer-events:none}.at-root.is-dj .at-deck-a{right:calc(100% - 5cqw - var(--dj-w));width:var(--dj-w)}.at-deck-b{width:var(--dj-w);opacity:0;transform:translate(50cqw,-50%) rotate(25deg)}.at-root.is-dj .at-deck-b{opacity:1;transform:translateY(-50%);transition-delay:.12s}.at-root.is-dj .at-vinyl-logo{animation:at-logo-out .8s cubic-bezier(.6,0,.4,1) forwards}@keyframes at-logo-out{to{opacity:0;transform:translate(-45vw,-25vh) rotate(-280deg) scale(.3)}}.at-mixer{--deck-accent: var(--at-accent);position:absolute;top:50%;left:50%;z-index:2;width:320px;margin-top:30px;display:flex;flex-direction:column;gap:16px;padding:18px;border:1px solid rgba(255,255,255,.09);border-radius:10px;background:linear-gradient(#18181bd1,#0c0c0ee6);box-shadow:0 30px 80px #0009,inset 0 1px #ffffff0d;-webkit-backdrop-filter:blur(12px);backdrop-filter:blur(12px);opacity:0;transform:translate(-50%,-50%) translateY(80px) scale(.9);transition:opacity .6s ease,transform .9s var(--dj-ease)}.at-root.is-dj .at-mixer{opacity:1;transform:translate(-50%,-50%);transition-delay:.25s}.at-mixer-strips{display:grid;grid-template-columns:1fr 1fr}.at-mixer-strip{display:grid;grid-template-columns:1fr 1fr;justify-items:center;gap:12px 6px;padding:0 10px}.at-mixer-strip+.at-mixer-strip{border-left:1px solid rgba(255,255,255,.08)}.at-mixer-ch{grid-column:1 / -1;font-family:var(--at-font-display);font-size:22px;line-height:1;color:var(--at-accent)}.at-knob-wrap{display:flex;flex-direction:column;align-items:center;gap:6px;font-family:var(--at-font-mono);font-size:9px;letter-spacing:.16em;text-transform:uppercase;color:var(--at-muted)}.at-knob{position:relative;width:40px;height:40px;border-radius:50%;background:radial-gradient(circle at 50% 32%,#3a3a3e,#151517 70%);box-shadow:inset 0 1px #ffffff24,0 4px 10px #0009,0 0 0 3px #0c0c0e,0 0 0 4px #ffffff14;cursor:ns-resize;touch-action:none}.at-knob:after{content:"";position:absolute;top:4px;left:50%;width:3px;height:12px;margin-left:-1.5px;border-radius:2px;background:var(--at-accent);box-shadow:0 0 6px var(--at-accent);transform-origin:50% 16px;transform:rotate(var(--knob, 0deg))}.at-knob:focus-visible{outline:2px solid var(--at-accent);outline-offset:5px}.at-mixer-faderbox{grid-column:1 / -1;display:flex;gap:14px;height:120px}.at-mixer-vu{display:flex;align-items:flex-end;width:8px;border-radius:4px;background:#ffffff0f;overflow:hidden}.at-mixer-vu-fill{width:100%;height:100%;background:linear-gradient(to top,#2bd9a0,#c2ff3a 55%,#ffc23d 78%,#ff4d3d);transform:scaleY(0);transform-origin:bottom}.at-mixer-vfader{position:relative;width:22px}.at-mixer-vfader input{position:absolute;top:50%;left:50%;width:120px;transform:translate(-50%,-50%) rotate(-90deg)}.at-mixer-xfader input::-webkit-slider-thumb{width:22px}.at-mixer-xfader input::-moz-range-thumb{width:22px}@container (max-width: 1000px){.at-deck{position:relative;top:auto;right:auto;width:min(84cqw,460px);margin:64px auto 20px;transform:none}.at-deck-b,.at-mixer,.at-root.is-dj .at-hero{display:none}.at-root.is-dj .at-deck-a,.at-root.is-dj .at-deck-b{display:block;right:auto;width:min(92cqw,460px);margin:24px auto;transform:none}.at-root.is-dj .at-mixer{position:relative;top:auto;left:auto;display:flex;width:min(92cqw,460px);margin:24px auto;transform:none}}@container (min-width: 1001px){.at-root:not(.has-hero):not(.is-dj) .at-deck-a{right:calc(50% - var(--w-single) / 2)}}.at-deck-label{overflow:hidden;display:grid;place-items:center;background:var(--deck-accent)}.at-deck-label img{width:100%;height:100%;object-fit:cover}.at-deck-label span{padding:12%;font-family:var(--at-font-display);font-size:clamp(11px,1.6cqw,20px);line-height:1.05;text-align:center;text-transform:uppercase;color:#0a0a0b;overflow-wrap:anywhere}.at-deck-sleeves{overflow-x:auto;padding:6px 4px 8px;scrollbar-width:thin}.at-deck-sleeve{flex:none}.at-deck-sleeve span{display:grid;place-items:center;width:100%;height:100%;padding:4px;background:var(--sleeve-accent, var(--at-accent));color:#0a0a0b;font-family:var(--at-font-mono);font-size:9px;line-height:1.1;overflow:hidden}.at-deck-sleeve[aria-pressed=true]{border-color:var(--sleeve-accent, var(--deck-accent))}.at-deck-upload{border:2px dashed rgba(255,255,255,.28);color:var(--at-muted);font-size:28px;opacity:.8}.at-deck-upload:hover{border-color:var(--at-accent);color:var(--at-accent)}.at-deck-drop{position:absolute;inset:0;z-index:3;display:grid;place-items:center;border:2px dashed var(--at-accent);border-radius:50%;background:#0a0a0bb8;font-family:var(--at-font-mono);font-size:12px;letter-spacing:.2em;text-transform:uppercase;color:var(--at-accent);pointer-events:none}.at-deck-band a{color:inherit;text-decoration:none;border-bottom:1px solid color-mix(in srgb,var(--deck-accent) 45%,transparent)}.at-deck-band a:hover{border-color:var(--deck-accent)}.at-mixer-ch{color:var(--at-accent)}.at-powered{display:flex;align-items:center;justify-content:center;gap:6px;font-family:var(--at-font-mono);font-size:10px;letter-spacing:.16em;text-transform:uppercase;color:var(--at-muted);text-decoration:none;transition:color .25s}.at-powered img{width:18px;height:18px}.at-powered:hover{color:var(--at-text)}.at-deck-btn,.at-pad,.at-dj-toggle{font-family:var(--at-font-mono)}.at-deck-btn{font-family:inherit}@media (prefers-reduced-motion: reduce){.at-vinyl-logo,.at-deck-disc{animation:none}}';
class xn extends HTMLElement {
  static observedAttributes = ["records", "decks", "mode", "uploads", "logo", "lang"];
  mount;
  list = [];
  loadId = 0;
  get records() {
    return this.list;
  }
  set records(e) {
    this.loadId++, this.list = Oi(e, document.baseURI), this.update();
  }
  connectedCallback() {
    if (!this.shadowRoot) {
      const e = this.attachShadow({ mode: "open" }), i = document.createElement("style");
      i.textContent = vn, this.mount = document.createElement("div"), e.append(i, this.mount);
    }
    this.update();
  }
  disconnectedCallback() {
    this.mount && ci(null, this.mount);
  }
  attributeChangedCallback(e, i, a) {
    e === "records" && a && a !== i ? this.fetchRecords(a) : this.update();
  }
  async fetchRecords(e) {
    const i = ++this.loadId;
    try {
      const a = new URL(e, document.baseURI).href, n = await (await fetch(a)).json();
      if (i !== this.loadId) return;
      this.list = Oi(n, a), this.update();
    } catch (a) {
      console.error("[audiola-turntable] records konnten nicht geladen werden:", a);
    }
  }
  update() {
    if (!this.mount || !this.isConnected) return;
    const e = this.getAttribute("logo");
    ci(
      /* @__PURE__ */ m(
        bn,
        {
          host: this,
          records: this.list,
          decks: this.getAttribute("decks") === "1" ? 1 : 2,
          mode: this.getAttribute("mode") === "dj" ? "dj" : "single",
          uploads: this.hasAttribute("uploads"),
          logo: e ? new URL(e, document.baseURI).href : null,
          lang: this.getAttribute("lang")
        }
      ),
      this.mount
    );
  }
}
customElements.get("audiola-turntable") || customElements.define("audiola-turntable", xn);
