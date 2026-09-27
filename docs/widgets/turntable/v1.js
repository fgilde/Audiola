var wt, V, Wn, Pe, ln, $n, Vn, Dt, ft, at, Yn, Kt, Ut, Wt, bt = {}, vt = [], ka = /acit|ex(?:s|g|n|p|$)|rph|grid|ows|mnc|ntw|ine[ch]|zoo|^ord|itera/i, At = Array.isArray;
function Ce(t, e) {
  for (var n in e) t[n] = e[n];
  return t;
}
function Qt(t) {
  t && t.parentNode && t.parentNode.removeChild(t);
}
function wa(t, e, n) {
  var a, i, r, s = {};
  for (r in e) r == "key" ? a = e[r] : r == "ref" ? i = e[r] : s[r] = e[r];
  if (arguments.length > 2 && (s.children = arguments.length > 3 ? wt.call(arguments, 2) : n), typeof t == "function" && t.defaultProps != null) for (r in t.defaultProps) s[r] === void 0 && (s[r] = t.defaultProps[r]);
  return _t(t, s, a, i, null);
}
function _t(t, e, n, a, i) {
  var r = { type: t, props: e, key: n, ref: a, __k: null, __: null, __b: 0, __e: null, __c: null, constructor: void 0, __v: i ?? ++Wn, __i: -1, __u: 0 };
  return i == null && V.vnode != null && V.vnode(r), r;
}
function Ge(t) {
  return t.children;
}
function pt(t, e) {
  this.props = t, this.context = e;
}
function je(t, e) {
  if (e == null) return t.__ ? je(t.__, t.__i + 1) : null;
  for (var n; e < t.__k.length; e++) if ((n = t.__k[e]) != null && n.__e != null) return n.__e;
  return typeof t.type == "function" ? je(t) : null;
}
function Aa(t) {
  if (t.__P && t.__d) {
    var e = t.__v, n = e.__e, a = [], i = [], r = Ce({}, e);
    r.__v = e.__v + 1, V.vnode && V.vnode(r), Jt(t.__P, r, e, t.__n, t.__P.namespaceURI, 32 & e.__u ? [n] : null, a, n ?? je(e), !!(32 & e.__u), i), r.__v = e.__v, r.__.__k[r.__i] = r, ea(a, r, i), e.__e = e.__ = null, r.__e != n && Kn(r);
  }
}
function Kn(t) {
  if ((t = t.__) != null && t.__c != null) return t.__e = t.__c.base = null, t.__k.some(function(e) {
    if (e != null && e.__e != null) return t.__e = t.__c.base = e.__e;
  }), Kn(t);
}
function cn(t) {
  (!t.__d && (t.__d = !0) && Pe.push(t) && !xt.__r++ || ln != V.debounceRendering) && ((ln = V.debounceRendering) || $n)(xt);
}
function xt() {
  try {
    for (var t, e = 1; Pe.length; ) Pe.length > e && Pe.sort(Vn), t = Pe.shift(), e = Pe.length, Aa(t);
  } finally {
    Pe.length = xt.__r = 0;
  }
}
function Qn(t, e, n, a, i, r, s, l, d, u, _) {
  var g, c, p, v, A, C, o = a && a.__k || vt, y = e.length;
  for (d = La(n, e, o, d, y), g = 0; g < y; g++) (p = n.__k[g]) != null && (c = p.__i != -1 && o[p.__i] || bt, p.__i = g, C = Jt(t, p, c, i, r, s, l, d, u, _), v = p.__e, p.ref && c.ref != p.ref && (c.ref && Zt(c.ref, null, p), _.push(p.ref, p.__c || v, p)), A == null && v != null && (A = v), 4 & p.__u ? (d = Jn(p, d, t), c.__e && (c.__e = null)) : typeof p.type == "function" && C !== void 0 ? d = C : v && (d = v.nextSibling), p.__u &= -7);
  return n.__e = A, d;
}
function La(t, e, n, a, i) {
  var r, s, l, d, u, _ = n.length, g = _, c = 0;
  for (t.__k = new Array(i), r = 0; r < i; r++) (s = e[r]) != null && typeof s != "boolean" && typeof s != "function" ? (typeof s == "string" || typeof s == "number" || typeof s == "bigint" || s.constructor == String ? s = t.__k[r] = _t(null, s, null, null, null) : At(s) ? s = t.__k[r] = _t(Ge, { children: s }, null, null, null) : s.constructor === void 0 && s.__b > 0 ? s = t.__k[r] = _t(s.type, s.props, s.key, s.ref ? s.ref : null, s.__v) : t.__k[r] = s, d = r + c, s.__ = t, s.__b = t.__b + 1, l = null, (u = s.__i = Ra(s, n, d, g)) != -1 && (g--, (l = n[u]) && (l.__u |= 2)), l == null || l.__v == null ? (u == -1 && (i > _ ? c-- : i < _ && c++), typeof s.type != "function" && (s.__u |= 4)) : u != d && (u == d - 1 ? c-- : u == d + 1 ? c++ : (u > d ? c-- : c++, s.__u |= 4))) : t.__k[r] = null;
  if (g) for (r = 0; r < _; r++) (l = n[r]) != null && !(2 & l.__u) && (l.__e == a && (a = je(l)), na(l, l));
  return a;
}
function Jn(t, e, n) {
  var a, i;
  if (typeof t.type == "function") {
    for (a = t.__k, i = 0; a && i < a.length; i++) a[i] && (a[i].__ = t, e = Jn(a[i], e, n));
    return e;
  }
  t.__e != e && (e && t.type && !e.parentNode && (e = je(t)), e = n.insertBefore(t.__e, e || null));
  do
    e = e && e.nextSibling;
  while (e != null && e.nodeType == 8);
  return e;
}
function Ra(t, e, n, a) {
  var i, r, s, l = t.key, d = t.type, u = e[n], _ = u != null && (2 & u.__u) == 0;
  if (u === null && l == null || _ && l == u.key && d == u.type) return n;
  if (a > (_ ? 1 : 0)) {
    for (i = n - 1, r = n + 1; i >= 0 || r < e.length; ) if ((u = e[s = i >= 0 ? i-- : r++]) != null && !(2 & u.__u) && l == u.key && d == u.type) return s;
  }
  return -1;
}
function dn(t, e, n) {
  e[0] == "-" ? t.setProperty(e, n ?? "") : t[e] = n == null ? "" : typeof n != "number" || ka.test(e) ? n : n + "px";
}
function lt(t, e, n, a, i) {
  var r, s;
  e: if (e == "style") if (typeof n == "string") t.style.cssText = n;
  else {
    if (typeof a == "string" && (t.style.cssText = a = ""), a) for (e in a) n && e in n || dn(t.style, e, "");
    if (n) for (e in n) a && n[e] == a[e] || dn(t.style, e, n[e]);
  }
  else if (e[0] == "o" && e[1] == "n") r = e != (e = e.replace(Yn, "$1")), s = e.toLowerCase(), e = s in t || e == "onFocusOut" || e == "onFocusIn" ? s.slice(2) : e.slice(2), t.l || (t.l = {}), t.l[e + r] = n, n ? a ? n[at] = a[at] : (n[at] = Kt, t.addEventListener(e, r ? Wt : Ut, r)) : t.removeEventListener(e, r ? Wt : Ut, r);
  else {
    if (i == "http://www.w3.org/2000/svg") e = e.replace(/xlink(H|:h)/, "h").replace(/sName$/, "s");
    else if (e != "width" && e != "height" && e != "href" && e != "list" && e != "form" && e != "tabIndex" && e != "download" && e != "rowSpan" && e != "colSpan" && e != "role" && e != "popover" && e in t) try {
      t[e] = n ?? "";
      break e;
    } catch {
    }
    typeof n == "function" || (n == null || n === !1 && e[4] != "-" ? t.removeAttribute(e) : t.setAttribute(e, e == "popover" && n == 1 ? "" : n));
  }
}
function hn(t) {
  return function(e) {
    if (this.l) {
      var n = this.l[e.type + t];
      if (e[ft] == null) e[ft] = Kt++;
      else if (e[ft] < n[at]) return;
      return n(V.event ? V.event(e) : e);
    }
  };
}
function Jt(t, e, n, a, i, r, s, l, d, u) {
  var _, g, c, p, v, A, C, o, y, k, x, q, D, E, z, F, O = e.type;
  if (e.constructor !== void 0) return null;
  128 & n.__u && (d = !!(32 & n.__u), r = [l = e.__e = n.__e]), (_ = V.__b) && _(e);
  e: if (typeof O == "function") {
    g = s.length;
    try {
      if (y = e.props, k = O.prototype && O.prototype.render, x = (_ = O.contextType) && a[_.__c], q = _ ? x ? x.props.value : _.__ : a, n.__c ? o = (c = e.__c = n.__c).__ = c.__E : (k ? e.__c = c = new O(y, q) : (e.__c = c = new pt(y, q), c.constructor = O, c.render = Ca), x && x.sub(c), c.state || (c.state = {}), c.__n = a, p = c.__d = !0, c.__h = [], c._sb = []), k && c.__s == null && (c.__s = c.state), k && O.getDerivedStateFromProps != null && (c.__s == c.state && (c.__s = Ce({}, c.__s)), Ce(c.__s, O.getDerivedStateFromProps(y, c.__s))), v = c.props, A = c.state, c.__v = e, p) k && O.getDerivedStateFromProps == null && c.componentWillMount != null && c.componentWillMount(), k && c.componentDidMount != null && c.__h.push(c.componentDidMount);
      else {
        if (k && O.getDerivedStateFromProps == null && y !== v && c.componentWillReceiveProps != null && c.componentWillReceiveProps(y, q), e.__v == n.__v || !c.__e && c.shouldComponentUpdate != null && c.shouldComponentUpdate(y, c.__s, q) === !1) {
          e.__v != n.__v && (c.props = y, c.state = c.__s, c.__d = !1), e.__e = n.__e, e.__k = n.__k, e.__k.some(function(h) {
            h && (h.__ = e);
          }), vt.push.apply(c.__h, c._sb), c._sb = [], c.__h.length && s.push(c), l = je(n);
          break e;
        }
        c.componentWillUpdate != null && c.componentWillUpdate(y, c.__s, q), k && c.componentDidUpdate != null && c.__h.push(function() {
          c.componentDidUpdate(v, A, C);
        });
      }
      if (c.context = q, c.props = y, c.__P = t, c.__e = !1, D = V.__r, E = 0, k) c.state = c.__s, c.__d = !1, D && D(e), _ = c.render(c.props, c.state, c.context), vt.push.apply(c.__h, c._sb), c._sb = [];
      else do
        c.__d = !1, D && D(e), _ = c.render(c.props, c.state, c.context), c.state = c.__s;
      while (c.__d && ++E < 25);
      c.state = c.__s, c.getChildContext != null && (a = Ce(Ce({}, a), c.getChildContext())), k && !p && c.getSnapshotBeforeUpdate != null && (C = c.getSnapshotBeforeUpdate(v, A)), z = _ != null && _.type === Ge && _.key == null ? ta(_.props.children) : _, l = Qn(t, At(z) ? z : [z], e, n, a, i, r, s, l, d, u), c.base = e.__e, e.__u &= -161, c.__h.length && s.push(c), o && (c.__E = c.__ = null);
    } catch (h) {
      if (s.length = g, e.__v = null, d || r != null) {
        if (h.then) {
          for (e.__u |= d ? 160 : 128; l && l.nodeType == 8 && l.nextSibling; ) l = l.nextSibling;
          r != null && (r[r.indexOf(l)] = null), e.__e = l;
        } else if (r != null) for (F = r.length; F--; ) Qt(r[F]);
      } else e.__e = n.__e;
      e.__k == null && (e.__k = n.__k || []), h.then || Zn(e), V.__e(h, e, n);
    }
  } else r == null && e.__v == n.__v ? (e.__k = n.__k, e.__e = n.__e) : l = e.__e = Sa(n.__e, e, n, a, i, r, s, d, u);
  return (_ = V.diffed) && _(e), 128 & e.__u ? void 0 : l;
}
function Zn(t) {
  t && (t.__c && (t.__c.__e = !0), t.__k && t.__k.some(Zn));
}
function ea(t, e, n) {
  for (var a = 0; a < n.length; a++) Zt(n[a], n[++a], n[++a]);
  V.__c && V.__c(e, t), t.some(function(i) {
    try {
      t = i.__h, i.__h = [], t.some(function(r) {
        r.call(i);
      });
    } catch (r) {
      V.__e(r, i.__v);
    }
  });
}
function ta(t) {
  return typeof t != "object" || t == null || t.__b > 0 ? t : At(t) ? t.map(ta) : t.constructor !== void 0 ? null : Ce({}, t);
}
function Sa(t, e, n, a, i, r, s, l, d) {
  var u, _, g, c, p, v, A, C = n.props || bt, o = e.props, y = e.type;
  if (y == "svg" ? i = "http://www.w3.org/2000/svg" : y == "math" ? i = "http://www.w3.org/1998/Math/MathML" : i || (i = "http://www.w3.org/1999/xhtml"), r != null) {
    for (u = 0; u < r.length; u++) if ((p = r[u]) && "setAttribute" in p == !!y && (y ? p.localName == y : p.nodeType == 3)) {
      t = p, r[u] = null;
      break;
    }
  }
  if (t == null) {
    if (y == null) return document.createTextNode(o);
    t = document.createElementNS(i, y, o.is && o), l && (V.__m && V.__m(e, r), l = !1), r = null;
  }
  if (y == null) C === o || l && t.data == o || (t.data = o);
  else {
    if (r = y == "textarea" && o.defaultValue != null ? null : r && wt.call(t.childNodes), !l && r != null) for (C = {}, u = 0; u < t.attributes.length; u++) C[(p = t.attributes[u]).name] = p.value;
    for (u in C) p = C[u], u == "dangerouslySetInnerHTML" ? g = p : u == "children" || u in o || u == "value" && "defaultValue" in o || u == "checked" && "defaultChecked" in o || lt(t, u, null, p, i);
    for (u in o) p = o[u], u == "children" ? c = p : u == "dangerouslySetInnerHTML" ? _ = p : u == "value" ? v = p : u == "checked" ? A = p : l && typeof p != "function" || C[u] === p || lt(t, u, p, C[u], i);
    if (_) l || g && (_.__html == g.__html || _.__html == t.innerHTML) || (t.innerHTML = _.__html), e.__k = [];
    else if (g && (t.innerHTML = ""), Qn(e.type == "template" ? t.content : t, At(c) ? c : [c], e, n, a, y == "foreignObject" ? "http://www.w3.org/1999/xhtml" : i, r, s, r ? r[0] : n.__k && je(n, 0), l, d), r != null) for (u = r.length; u--; ) Qt(r[u]);
    l && y != "textarea" || (u = "value", y == "progress" && v == null ? t.removeAttribute("value") : v != null && (v !== t[u] || y == "progress" && !v || y == "option" && v != C[u]) && lt(t, u, v, C[u], i), u = "checked", A != null && A != t[u] && lt(t, u, A, C[u], i));
  }
  return t;
}
function Zt(t, e, n) {
  try {
    if (typeof t == "function") {
      var a = typeof t.__u == "function";
      a && t.__u(), a && e == null || (t.__u = t(e));
    } else t.current = e;
  } catch (i) {
    V.__e(i, n);
  }
}
function na(t, e, n) {
  var a, i;
  if (V.unmount && V.unmount(t), (a = t.ref) && (a.current && a.current != t.__e || Zt(a, null, e)), (a = t.__c) != null) {
    if (a.componentWillUnmount) try {
      a.componentWillUnmount();
    } catch (r) {
      V.__e(r, e);
    }
    a.base = a.__P = a.__n = null;
  }
  if (a = t.__k) for (i = 0; i < a.length; i++) a[i] && na(a[i], e, n || typeof t.type != "function");
  n || Qt(t.__e), t.__c = t.__ = t.__e = void 0;
}
function Ca(t, e, n) {
  return this.constructor(t, n);
}
function un(t, e, n) {
  var a, i, r, s;
  e == document && (e = document.documentElement), V.__ && V.__(t, e), i = (a = !1) ? null : e.__k, r = [], s = [], Jt(e, t = e.__k = wa(Ge, null, [t]), i || bt, bt, e.namespaceURI, i ? null : e.firstChild ? wt.call(e.childNodes) : null, r, i ? i.__e : e.firstChild, a, s), ea(r, t, s), t.props.children = null;
}
wt = vt.slice, V = { __e: function(t, e, n, a) {
  for (var i, r, s; e = e.__; ) if ((i = e.__c) && !i.__) try {
    if ((r = i.constructor) && r.getDerivedStateFromError != null && (i.setState(r.getDerivedStateFromError(t)), s = i.__d), i.componentDidCatch != null && (i.componentDidCatch(t, a || {}), s = i.__d), s) return i.__E = i;
  } catch (l) {
    t = l;
  }
  throw t;
} }, Wn = 0, pt.prototype.setState = function(t, e) {
  var n;
  n = this.__s != null && this.__s != this.state ? this.__s : this.__s = Ce({}, this.state), typeof t == "function" && (t = t(Ce({}, n), this.props)), t && Ce(n, t), t != null && this.__v && (e && this._sb.push(e), cn(this));
}, pt.prototype.forceUpdate = function(t) {
  this.__v && (this.__e = !0, t && this.__h.push(t), cn(this));
}, pt.prototype.render = Ge, Pe = [], $n = typeof Promise == "function" ? Promise.prototype.then.bind(Promise.resolve()) : setTimeout, Vn = function(t, e) {
  return t.__v.__b - e.__v.__b;
}, xt.__r = 0, Dt = Math.random().toString(8), ft = "__d" + Dt, at = "__a" + Dt, Yn = /(PointerCapture)$|Capture$/i, Kt = 0, Ut = hn(!1), Wt = hn(!0);
var Ea = 0;
function m(t, e, n, a, i, r) {
  e || (e = {});
  var s, l, d = e;
  if ("ref" in d) for (l in d = {}, e) l == "ref" ? s = e[l] : d[l] = e[l];
  var u = { type: t, props: d, key: n, ref: s, __k: null, __: null, __b: 0, __e: null, __c: null, constructor: void 0, __v: --Ea, __i: -1, __u: 0, __source: i, __self: r };
  if (typeof t == "function" && (s = t.defaultProps)) for (l in s) d[l] === void 0 && (d[l] = s[l]);
  return V.vnode && V.vnode(u), u;
}
var it, te, Pt, fn, yt = 0, aa = [], ae = V, _n = ae.__b, pn = ae.__r, gn = ae.diffed, mn = ae.__c, bn = ae.unmount, vn = ae.__;
function en(t, e) {
  ae.__h && ae.__h(te, t, yt || e), yt = 0;
  var n = te.__H || (te.__H = { __: [], __h: [] });
  return t >= n.__.length && n.__.push({}), n.__[t];
}
function He(t) {
  return yt = 1, Ta(ra, t);
}
function Ta(t, e, n) {
  var a = en(it++, 2);
  if (a.t = t, !a.__c && (a.__ = [ra(void 0, e), function(l) {
    var d = a.__N ? a.__N[0] : a.__[0], u = a.t(d, l);
    d !== u && (a.__N = [u, a.__[1]], a.__c.setState({}));
  }], a.__c = te, !te.__f)) {
    var i = function(l, d, u) {
      if (!a.__c.__H) return !0;
      var _ = !1, g = a.__c.props !== l;
      if (a.__c.__H.__.some(function(p) {
        if (p.__N) {
          _ = !0;
          var v = p.__[0];
          p.__ = p.__N, p.__N = void 0, v !== p.__[0] && (g = !0);
        }
      }), r) {
        var c = r.call(this, l, d, u);
        return _ ? c || g : c;
      }
      return !_ || g;
    };
    te.__f = !0;
    var r = te.shouldComponentUpdate, s = te.componentWillUpdate;
    te.componentWillUpdate = function(l, d, u) {
      if (this.__e) {
        var _ = r;
        r = void 0, i(l, d, u), r = _;
      }
      s && s.call(this, l, d, u);
    }, te.shouldComponentUpdate = i;
  }
  return a.__N || a.__;
}
function Ee(t, e) {
  var n = en(it++, 3);
  !ae.__s && ia(n.__H, e) && (n.__ = t, n.u = e, te.__H.__h.push(n));
}
function ue(t) {
  return yt = 5, Ma(function() {
    return { current: t };
  }, []);
}
function Ma(t, e) {
  var n = en(it++, 7);
  return ia(n.__H, e) && (n.__ = t(), n.__H = e, n.__h = t), n.__;
}
function Ba() {
  for (var t; t = aa.shift(); ) {
    var e = t.__H;
    if (t.__P && e) try {
      e.__h.some(gt), e.__h.some($t), e.__h = [];
    } catch (n) {
      e.__h = [], ae.__e(n, t.__v);
    }
  }
}
ae.__b = function(t) {
  te = null, _n && _n(t);
}, ae.__ = function(t, e) {
  t && e.__k && e.__k.__m && (t.__m = e.__k.__m), vn && vn(t, e);
}, ae.__r = function(t) {
  pn && pn(t), it = 0;
  var e = (te = t.__c).__H;
  e && (Pt === te ? (e.__h = [], te.__h = [], e.__.some(function(n) {
    n.__N && (n.__ = n.__N), n.u = n.__N = void 0;
  })) : (e.__h.some(gt), e.__h.some($t), e.__h = [], it = 0)), Pt = te;
}, ae.diffed = function(t) {
  gn && gn(t);
  var e = t.__c;
  e && e.__H && (e.__H.__h.length && (aa.push(e) !== 1 && fn === ae.requestAnimationFrame || ((fn = ae.requestAnimationFrame) || Da)(Ba)), e.__H.__.some(function(n) {
    n.u && (n.__H = n.u, n.u = void 0);
  })), Pt = te = null;
}, ae.__c = function(t, e) {
  e.some(function(n) {
    try {
      n.__h.some(gt), n.__h = n.__h.filter(function(a) {
        return !a.__ || $t(a);
      });
    } catch (a) {
      e.some(function(i) {
        i.__h && (i.__h = []);
      }), e = [], ae.__e(a, n.__v);
    }
  }), mn && mn(t, e);
}, ae.unmount = function(t) {
  bn && bn(t);
  var e, n = t.__c;
  n && n.__H && (n.__H.__.some(function(a) {
    try {
      gt(a);
    } catch (i) {
      e = i;
    }
  }), n.__H = void 0, e && ae.__e(e, n.__v));
};
var xn = typeof requestAnimationFrame == "function";
function Da(t) {
  var e, n = function() {
    clearTimeout(a), xn && cancelAnimationFrame(e), setTimeout(t);
  }, a = setTimeout(n, 35);
  xn && (e = requestAnimationFrame(n));
}
function gt(t) {
  var e = te, n = t.__c;
  typeof n == "function" && (t.__c = void 0, n()), te = e;
}
function $t(t) {
  var e = te;
  t.__c = t.__(), te = e;
}
function ia(t, e) {
  return !t || t.length !== e.length || e.some(function(n, a) {
    return n !== t[a];
  });
}
function ra(t, e) {
  return typeof e == "function" ? e(t) : e;
}
const Pa = `
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
class yn {
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
  attach(e, n) {
    this.ctx = e;
    const a = this.node = new AudioWorkletNode(e, "dr-deck", { outputChannelCount: [2] });
    a.port.onmessage = (i) => i.data.gen === this.gen && this.onPosition(i.data.pos, i.data.ended), this.eq = {
      low: new BiquadFilterNode(e, { type: "lowshelf", frequency: 220 }),
      mid: new BiquadFilterNode(e, { type: "peaking", frequency: 1e3, Q: 0.9 }),
      high: new BiquadFilterNode(e, { type: "highshelf", frequency: 3500 })
    }, this.filter = new BiquadFilterNode(e, { type: "allpass" }), this.fader = new GainNode(e), this.xf = new GainNode(e), this.analyser = new AnalyserNode(e, { fftSize: 512 }), a.connect(this.eq.low).connect(this.eq.mid).connect(this.eq.high).connect(this.filter).connect(this.fader).connect(this.xf).connect(n), this.xf.connect(this.analyser);
  }
  /** Kanalsignal nach Fader + Crossfader (für Ring-Visual); erst nach `unlock()` gesetzt. */
  get output() {
    return this.xf;
  }
  /**
   * Lädt einen Track und springt auf `offset(dauer)` Sekunden. Liefert die Dauer oder null,
   * wenn inzwischen ein anderer Track angefordert wurde.
   */
  async load(e, n) {
    const a = ++this.gen;
    await this.engine.unlock();
    const i = await fetch(e);
    if (!i.ok) throw new Error(`${i.status} ${e}`);
    const r = await this.ctx.decodeAudioData(await i.arrayBuffer());
    if (a !== this.gen) return null;
    const s = r.getChannelData(0).slice(), l = r.getChannelData(r.numberOfChannels > 1 ? 1 : 0).slice();
    return this.node.port.postMessage({ L: s, R: l, gen: a, seek: n(r.duration) }, [s.buffer, l.buffer]), r.duration;
  }
  seek(e) {
    this.node?.port.postMessage({ seek: e });
  }
  setRate(e) {
    this.node && this.node.parameters.get("rate").setTargetAtTime(e, this.ctx.currentTime, 0.01);
  }
  /** -1 = Kill, 0 = neutral, 1 = +6 dB. */
  setEq(e, n) {
    this.eq && (this.eq[e].gain.value = n < 0 ? n * 26 : n * 6);
  }
  /** DJ-Filter: -1 = Lowpass zu, 0 = neutral, 1 = Highpass zu. */
  setFilter(e) {
    const n = this.filter;
    if (n) {
      if (Math.abs(e) < 0.03) {
        n.type = "allpass";
        return;
      }
      n.type = e < 0 ? "lowpass" : "highpass", n.frequency.value = e < 0 ? 2e4 * 2 ** (e * 10) : 20 * 2 ** (e * 10), n.Q.value = 1 + Math.abs(e) * 6;
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
    for (const n of this.meterBuf) e += n * n;
    return Math.min(1, Math.sqrt(e / this.meterBuf.length) * 3.2);
  }
  /** Alles auf neutral (beim Schließen des Mischpults). */
  reset() {
    for (const e of ["low", "mid", "high"]) this.setEq(e, 0);
    this.setFilter(0), this.setVolume(1);
  }
}
class Ia {
  ctx;
  out;
  echoSend;
  ready;
  channels = [new yn(this), new yn(this)];
  /** Erster Aufruf muss aus einer Nutzer-Geste kommen (Autoplay-Policy). */
  unlock() {
    if (!this.ready) {
      const e = this.ctx = new AudioContext(), n = URL.createObjectURL(new Blob([Pa], { type: "text/javascript" }));
      this.ready = e.audioWorklet.addModule(n).then(() => {
        const a = new GainNode(e), i = this.out = new GainNode(e), r = this.echoSend = new GainNode(e, { gain: 0 }), s = new DelayNode(e, { delayTime: 0.375 }), l = new GainNode(e, { gain: 0.55 });
        a.connect(i).connect(e.destination), a.connect(r).connect(s).connect(l).connect(s), s.connect(i), this.channels.forEach((d) => d.attach(e, a));
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
    const [n, a] = this.channels;
    n.setCrossGain(e <= 0 ? 1 : Math.cos(e * Math.PI / 2)), a.setCrossGain(e >= 0 ? 1 : Math.cos(-e * Math.PI / 2));
  }
  /** Echo-Send auf/zu; der Nachhall läuft nach dem Loslassen über das Feedback aus. */
  echo(e) {
    this.echoSend && this.echoSend.gain.setTargetAtTime(e ? 0.8 : 0, this.ctx.currentTime, 0.02);
  }
  /** Synthetisches Airhorn: gestapelte Sägezähne im klassischen „BAAP-BAP-BAP-BAAAP“-Rhythmus. */
  horn() {
    const e = this.ctx;
    if (!e || !this.out) return;
    const n = e.currentTime + 0.01, a = new BiquadFilterNode(e, { type: "lowpass", frequency: 2800, Q: 2 }), i = new GainNode(e, { gain: 0 });
    a.connect(i).connect(this.out);
    const r = [[0, 0.16], [0.2, 0.1], [0.34, 0.1], [0.48, 0.55]];
    for (const [s, l] of r)
      i.gain.setTargetAtTime(0.28, n + s, 8e-3), i.gain.setTargetAtTime(0, n + s + l, 0.02);
    for (const s of [466, 470, 700]) {
      const l = new OscillatorNode(e, { type: "sawtooth", frequency: s });
      l.frequency.setValueAtTime(s, n + 0.7), l.frequency.linearRampToValueAtTime(s * 0.94, n + 1.05), l.connect(a), l.start(n), l.stop(n + 1.2);
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
const Na = {
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
}, Fa = {
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
}, Oa = (t) => (t ?? navigator.language).toLowerCase().startsWith("de") ? Na : Fa, za = "data:image/webp;base64,UklGRiQIAABXRUJQVlA4WAoAAAAQAAAAPwAAPwAAQUxQSEMDAAABoEXbtilJ2ueem0Y4HRll27ZttW3btm3btm3bdpcd991zGpE58mX89eiPiJgA/MeZAMorxu6HgvOITOnPa6KG8sficNWTwXljqH619+syZPKFca06pzeA84QxSAKRwA8E58tz6kScPpcnjPnqREQCnQ/OAzLFn0sgqhLIF8WGWp/F/urkX+p0f9jWRATAmOSf3ufyfnnSGABErSMn4xx1KiIqok7PBaPVcv9akKFOm7yXpv2mTmQI9f2pFRAPSwMWd6qTZjq9HUxIDzYUHjAqg0IM1UD+raoiouIHgdF+LMLnbskBaVxc/5C6JkRFNq/Xp1BYUN830cWGRAWjGodlin67cbNKUyK6+oSsjs6Mb+jbMKqQwgH1iifRNlDJraoqEuhRX+lrBeWxLol+BmGS7Zbs0X7IlW/5QESb43X5Gq+Hde7So7FfalAhUYuBB1YPm7nFzGc1EHFNqajboN7rOXssGNErNbwQLUYdEm1i7csasIW6QLd/yQe5vP9ksfogmI6u0e6JzqkOaLF2sdqKulgy/q06XW+31U0iqiqb9Ghark5/TFTF0hXpeJuWS1fWlNfGcZg6p48W0H3qss5lnT5XgJvUOT0JiXRFQ0VNiwGmrqyhKP6n904P79ij8irNeWtt18wOGohfn7G1pfWMEKnEVuJM3ezcuu222HGqGXLag4+cPbJs3i5Ll/3pXFZvQjmXUBgAIb3aqerX20xuW8LIyaUdpu/4uapms/1gEDLjPtXfHjtmcBEAsGVjGQBKRp72wkrVN6yhcAw63HfwiCgAMpaZwUTWWDYEoHry0U8NgQkHKEBOg5yEptkSABQgbMa4x7uaS/rh4gEX3n/PIlzYM37dE7fUd7oUoIK7T0X4xjz1yQ1YPtf+MefXXbdcO+TTqfffO+zGFycst4XY4etvepAJiTFj1RbZxg9H4/NZ70Xxxv4PL/q5J6K/b/EOgB+PefJmcDhkij+757Q3brrx2aNW9lx92dW/ZL6ddNHn+71850g9dscDlp95kesJEw7iewDVO8cPOXswdjrmoAy2yWCnc/YtTO137K5bDgG2GBRSTovmEppvLcInSyAmaw2stQZMsNYSWctsADah/Q8jAFZQOCC6BAAAsBoAnQEqQABAAD4xEodDIiEKhzNyEAGCWwA0iPA78rY/wZkj/NPzAdpjxSOkB5gP146gHoAf0b+gdYB+wHsAeaj/vf3M+BP9p/22+Af9mv//rXJED1J8HXbz2oycz43QCf5N/eN4I49/K/9txgd5b+Vf5z0A7wbvT0Df7z2Sf5T/k+TX5u/6vuB/xr+e/6H+2fvH3afQn/VNzBBM9xPBL9vfpVmgJGY2Ylf6rPDYPaop45ctX9+5Yk1pCGa+FJrY90pBBy1Xb4+/6uUN2XR3bAuerktgjLtlSK7SZ3KAAP7/YCSv//pr2h/NpjzKYHzHrqzIe7wvV4CRniP9tgz6ts3zcL1VEA4VPF0kMrzA3j5zEPItpi6khSoifOGg53Fml1qHOwOkH02s0rMha1FY8VdGM6o4Behbm//1L95zlvDAowhwHOrqMtMB0oLuOAqiMqLj9/EAZCrw8hD7gjY3/93G0TQlHzFKUQb+zh6kX35taXWZPAQszQEp4gUNvUXPBmuaAmk9n5Cf3/Fh6mwW8/WXKzQ+0W7GC3wvlkvd+EtxDLRg0DKBDr6XnHywG5h10d4pP71dw+NpkKeoMJdrp8V/B4ZVWKav7+LsQzaSSETGablabdO+/7mHjIAlti5JvjqgOC8eLzLwN1hBfhKwIPS39u44WOXuBBW7c/hhyN+Sv7AVW9JQPXa+BDv/EuOmSEw7/vrkohIRqP2AfCAyQDurlyFeQS/fjMr2aPtlGsJDxbR8KYVixXcXttKGq7DtPhNVE1uhxo8IkzuCK62eoXAskrgD/osT6KDkXK9GAZ69l6lcNeeD0rGAG7qLmJm5PzjK83An3/N30wQRaqLVCfsrTNP/E3MBm1ppgA5e/P/Gaz4zpRLdAJevtWgqy5hHmYKSnNs4l6O3y6TjuFX4vsSMSIzs65WjlIq2DFqF+Ut2vlEEKUmWjweLVvZM8lfRoLtzJK//bZJJgkBEcuvaO3Wz9oTyccxRWU0Cz5yF4XMf/5TpzZXjlOEFIm9dH3hw5azGt/zP5KMScl+qVeuVqkKOw078teQ5KSop+yKc4d3e/J9iRlA16mdJr8JJ+8hRVwVbPAYiRXPlqB72Ax5TQmElnZ7phPOpX+e8viFOwuKPq5JUstTpyq93r+A+mkS/56jpY7Dwt8V2yecEvfjKJSFvjBTwBPEpE7FpoqNE12/IeaZKQ9uMXkIkU1WZ3LfgNOuPydJm/N1qvRyg4znsHhHJe+/3s73CCSVo2n+oaVtemKYzTuDU2IfoxyqEdkkYwgrga+IocQm70JWwutucmchFtULPs8Z/u3yLdtibk9vTciAqH9lAIelFMA8GYL//UvNmcLw2OPSSBXfasFbHwb+BvSfYS+tisTpOWKeX0GUTKj0jpt+CqdZuEg2pgfrrWLo1EHhyqTXm9JU46hDiUFOBBTi3W5d7RC0WS/NjYa7BPAKhM6diNB/sybHLX+E5phK/OQyhOSBt6WtVR5acwz0VZWy/pe720fDDne2vKayLkHa+utzKcqExmo0oHAsMAWf9PXzc+o5G/tqkqhdgoQaEKbeTcCYlBLUvxucgur3LrvmiHM4UQy/rxceDMX6XQI+VUr/eAAAAAA==";
function ct({ label: t, onChange: e }) {
  const [n, a] = He(0), i = ue(null), r = (l) => {
    const d = Math.max(-1, Math.min(1, Math.abs(l) < 0.04 ? 0 : l));
    a(d), e(d);
  }, s = (l) => {
    const d = { ArrowUp: 0.1, ArrowRight: 0.1, ArrowDown: -0.1, ArrowLeft: -0.1 }[l.key];
    d && (l.preventDefault(), r(n + d));
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
        "aria-valuenow": Math.round(n * 100) / 100,
        style: { "--knob": `${n * 135}deg` },
        onPointerDown: (l) => {
          l.currentTarget.setPointerCapture(l.pointerId), i.current = { y: l.clientY, v: n };
        },
        onPointerMove: (l) => i.current && r(i.current.v + (i.current.y - l.clientY) / 90),
        onPointerUp: () => i.current = null,
        onPointerCancel: () => i.current = null,
        onDblClick: () => r(0),
        onKeyDown: s
      }
    ),
    /* @__PURE__ */ m("span", { children: t })
  ] });
}
function qa(t) {
  const e = (n) => (a) => t.channel.setEq(n, a);
  return /* @__PURE__ */ m("div", { class: "at-mixer-strip", children: [
    /* @__PURE__ */ m("span", { class: "at-mixer-ch", children: t.name }),
    /* @__PURE__ */ m(ct, { label: "Hi", onChange: e("high") }),
    /* @__PURE__ */ m(ct, { label: "Mid", onChange: e("mid") }),
    /* @__PURE__ */ m(ct, { label: "Low", onChange: e("low") }),
    /* @__PURE__ */ m(ct, { label: "Filter", onChange: (n) => t.channel.setFilter(n) }),
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
          onInput: (n) => t.channel.setVolume(+n.currentTarget.value)
        }
      ) })
    ] })
  ] });
}
function Ha({ engine: t, active: e, t: n, onClose: a }) {
  const i = ue([]);
  Ee(() => {
    if (!e) return;
    let s = 0;
    const l = () => {
      t.channels.forEach((d, u) => {
        const _ = i.current[u];
        _ && (_.style.transform = `scaleY(${d.level()})`);
      }), s = requestAnimationFrame(l);
    };
    return s = requestAnimationFrame(l), () => cancelAnimationFrame(s);
  }, [e, t]);
  const r = (s) => void t.unlock().then(() => t.echo(s));
  return /* @__PURE__ */ m("div", { class: "at-mixer", children: [
    /* @__PURE__ */ m("button", { class: "at-dj-toggle", onClick: a, children: n.close }),
    /* @__PURE__ */ m("div", { class: "at-mixer-strips", children: t.channels.map((s, l) => /* @__PURE__ */ m(qa, { channel: s, name: l ? "B" : "A", t: n, meter: (d) => i.current[l] = d }, l)) }),
    /* @__PURE__ */ m("label", { class: "at-fader at-mixer-xfader", children: [
      /* @__PURE__ */ m("span", { children: n.crossfader }),
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
      n.poweredBy,
      " ",
      /* @__PURE__ */ m("img", { src: za, alt: "" }),
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
const Ga = "4.5.4", tn = Math.PI, We = 2 * tn, It = tn / 2, kn = 8.17579892, wn = "dual-combined", dt = "dual-horizontal", Se = "single", Ae = "dual-vertical", An = "bar-index", Ln = "bar-level", mt = "gradient", Rn = 60, Sn = "click", ja = "fullscreenchange", sa = "resize", Xa = "#111", oa = "", Cn = "A", En = "B", Tn = "C", Mn = "D", Bn = "468", ht = "sans-serif", Ua = "#0f0", Wa = "#7f7f7f22", $e = 10, Dn = "create", Pn = "fschange", $a = "lores", Va = sa, Nt = "user", Ya = "#000c", In = "#fff", Ka = "#4f4", Nn = "#888", Qa = "#555", Ft = "bark", et = "linear", Ye = "log", Ot = "mel", Fn = ["#a35", "#c66", "#e94", "#ed0", "#9d5", "#4d8", "#2cb", "#0bc", "#09c", "#36b"], la = [
  ["classic", {
    colorStops: [
      "red",
      { color: "yellow", level: 0.85, pos: 0.6 },
      { color: "lime", level: 0.475 }
    ]
  }],
  ["prism", {
    colorStops: Fn
  }],
  ["rainbow", {
    dir: "h",
    colorStops: ["#817", ...Fn, "#639"]
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
  channelLayout: Se,
  colorMode: mt,
  fadePeaks: !1,
  fftSize: 8192,
  fillAlpha: 1,
  frequencyScale: Ye,
  gradient: la[0][0],
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
  weightingFilter: oa,
  width: void 0
}, Ja = ["ERR_AUDIO_CONTEXT_FAIL", "Could not create audio context. Web Audio API not supported?"], Za = ["ERR_INVALID_AUDIO_CONTEXT", "Provided audio context is not valid"], ei = ["ERR_UNKNOWN_GRADIENT", "Unknown gradient"], zt = ["ERR_FREQUENCY_TOO_LOW", "Frequency values must be >= 1"], ti = ["ERR_INVALID_MODE", "Invalid mode"], ni = ["ERR_REFLEX_OUT_OF_RANGE", "Reflex ratio must be >= 0 and < 1"], ai = ["ERR_INVALID_AUDIO_SOURCE", "Audio source must be an instance of HTMLMediaElement or AudioNode"], ii = ["ERR_GRADIENT_INVALID_NAME", "Gradient name must be a non-empty string"], ri = ["ERR_GRADIENT_NOT_AN_OBJECT", "Gradient options must be an object"], si = ["ERR_GRADIENT_MISSING_COLOR", "Gradient colorStops must be a non-empty array"];
class me extends Error {
  constructor(e, n) {
    const [a, i] = e;
    super(i + (n !== void 0 ? `: ${n}` : "")), this.name = "AudioMotionError", this.code = a;
  }
}
const On = (t, e) => console.warn(`${t} is deprecated. Use ${e} instead.`), zn = (t) => {
  for (const e in t)
    return !1;
  return !0;
}, ut = (t, e, n = "toLowerCase") => e[Math.max(0, e.indexOf(("" + t)[n]()))], oi = (t, e, n, a, i) => e + (a - e) * (i - t) / (n - t);
Array.prototype.findLastIndex || (Array.prototype.findLastIndex = function(t) {
  let e = this.length;
  for (; e-- > 0; )
    if (t(this[e]))
      return e;
  return -1;
});
class li {
  /**
   * CONSTRUCTOR
   *
   * @param {object} [container] DOM element where to insert the analyzer; if undefined, uses the document body
   * @param {object} [options]
   * @returns {object} AudioMotionAnalyzer object
   */
  constructor(e, n = {}) {
    this._ready = !1, this._aux = {}, this._canvasGradients = [], this._destroyed = !1, this._energy = { val: 0, peak: 0, hold: 0 }, this._flg = {}, this._fps = 0, this._gradients = {}, this._last = 0, this._outNodes = [], this._ownContext = !1, this._selectedGrads = [], this._sources = [], e instanceof Element || (zn(n) && !zn(e) && (n = e), e = null), this._ownCanvas = !(n.canvas instanceof HTMLCanvasElement);
    const a = this._ownCanvas ? document.createElement("canvas") : n.canvas;
    a.style = "max-width: 100%;", this._ctx = a.getContext("2d");
    for (const [g, c] of la)
      this.registerGradient(g, c);
    this._container = e || !this._ownCanvas && a.parentElement || document.body, this._defaultWidth = this._container.clientWidth || 640, this._defaultHeight = this._container.clientHeight || 270;
    let i;
    if (!(n.source && (i = n.source.context))) {
      if (!(i = n.audioCtx)) try {
        i = new (window.AudioContext || window.webkitAudioContext)(), this._ownContext = !0;
      } catch {
        throw new me(Ja);
      }
    }
    if (!i.createGain)
      throw new me(Za);
    const r = this._analyzer = [i.createAnalyser(), i.createAnalyser()], s = this._splitter = i.createChannelSplitter(2), l = this._merger = i.createChannelMerger(2);
    this._input = i.createGain(), this._output = i.createGain(), n.source && this.connectInput(n.source);
    for (const g of [0, 1])
      s.connect(r[g], g);
    l.connect(this._output), n.connectSpeakers !== !1 && this.connectOutput();
    for (const g of ["_scaleX", "_scaleR"])
      this[g] = document.createElement("canvas").getContext("2d");
    this._fsEl = n.fsElement || a;
    const d = () => {
      this._fsTimeout || (this._fsTimeout = window.setTimeout(() => {
        this._fsChanging || (this._setCanvas(Va), this._fsTimeout = 0);
      }, Rn));
    };
    window.ResizeObserver && (this._observer = new ResizeObserver(d), this._observer.observe(this._container)), this._controller = new AbortController();
    const u = this._controller.signal;
    window.addEventListener(sa, d, { signal: u }), a.addEventListener(ja, () => {
      this._fsChanging = !0, this._fsTimeout && window.clearTimeout(this._fsTimeout), this._setCanvas(Pn), this._fsTimeout = window.setTimeout(() => {
        this._fsChanging = !1, this._fsTimeout = 0;
      }, Rn);
    }, { signal: u });
    const _ = () => {
      i.state == "suspended" && i.resume().then(() => window.removeEventListener(Sn, _));
    };
    window.addEventListener(Sn, _), document.addEventListener("visibilitychange", () => {
      document.visibilityState != "hidden" && (this._frames = 0, this._time = performance.now());
    }, { signal: u }), this._setProps(n, !0), this.useCanvas && this._ownCanvas && this._container.appendChild(a), this._ready = !0, this._setCanvas(Dn);
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
    this._chLayout = ut(e, [Se, dt, Ae, wn]), this._input.disconnect(), this._input.connect(this._chLayout != Se ? this._splitter : this._analyzer[0]), this._analyzer[0].disconnect(), this._outNodes.length && this._analyzer[0].connect(this._chLayout != Se ? this._merger : this._output), this._calcBars(), this._makeGrad();
  }
  get colorMode() {
    return this._colorMode;
  }
  set colorMode(e) {
    this._colorMode = ut(e, [mt, An, Ln]);
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
    const n = this._analyzer[0].frequencyBinCount;
    this._fftData = [new Float32Array(n), new Float32Array(n)], this._calcBars();
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
    this._loRes = !!e, this._setCanvas($a);
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
    for (const n of [0, 1])
      this._analyzer[n].maxDecibels = e;
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
      throw new me(zt);
    this._maxFreq = Math.min(e, this.audioCtx.sampleRate / 2), this._calcBars();
  }
  get minDecibels() {
    return this._analyzer[0].minDecibels;
  }
  set minDecibels(e) {
    for (const n of [0, 1])
      this._analyzer[n].minDecibels = e;
  }
  get minFreq() {
    return this._minFreq;
  }
  set minFreq(e) {
    if (e < 1)
      throw new me(zt);
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
    const n = e | 0;
    if (n >= 0 && n <= 10 && n != 9)
      this._mode = n, this._calcBars(), this._makeGrad();
    else
      throw new me(ti, e);
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
      throw new me(ni);
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
    for (const n of [0, 1])
      this._analyzer[n].smoothingTimeConstant = e;
  }
  get spinSpeed() {
    return this._spinSpeed;
  }
  set spinSpeed(e) {
    e = +e || 0, (this._spinSpeed === void 0 || e == 0) && (this._spinAngle = -It), this._spinSpeed = e;
  }
  get splitGradient() {
    return this._splitGradient;
  }
  set splitGradient(e) {
    this._splitGradient = !!e, this._makeGrad();
  }
  get stereo() {
    return On("stereo", "channelLayout"), this._chLayout != Se;
  }
  set stereo(e) {
    On("stereo", "channelLayout"), this.channelLayout = e ? Ae : Se;
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
    this._weightingFilter = ut(e, [oa, Cn, En, Tn, Mn, Bn], "toUpperCase");
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
    return Ga;
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
    const n = e instanceof HTMLMediaElement;
    if (!(n || e.connect))
      throw new me(ai);
    const a = n ? this.audioCtx.createMediaElementSource(e) : e;
    return this._sources.includes(a) || (a.connect(this._input), this._sources.push(a)), a;
  }
  /**
   * Connects the analyzer output to another audio node
   *
   * @param [{object}] an AudioNode; if undefined, the output is connected to the audio context destination (speakers)
   */
  connectOutput(e = this.audioCtx.destination) {
    if (!this._outNodes.includes(e) && (this._output.connect(e), this._outNodes.push(e), this._outNodes.length == 1))
      for (const n of [0, 1])
        this._analyzer[n].connect(this._chLayout == Se && !n ? this._output : this._merger, 0, n);
  }
  /**
   * Destroys instance
   */
  destroy() {
    if (!this._ready)
      return;
    const { audioCtx: e, canvas: n, _controller: a, _input: i, _merger: r, _observer: s, _ownCanvas: l, _ownContext: d, _splitter: u } = this;
    this._destroyed = !0, this._ready = !1, this.stop(), a.abort(), s && s.disconnect(), this.onCanvasResize = null, this.onCanvasDraw = null, this._fsEl = null, this.disconnectInput(), this.disconnectOutput(), i.disconnect(), u.disconnect(), r.disconnect(), d && e.close(), l && n.remove(), this._calcBars();
  }
  /**
   * Disconnects audio sources from the analyzer
   *
   * @param [{object|array}] a connected AudioNode object or an array of such objects; if falsy, all connected nodes are disconnected
   * @param [{boolean}] if true, stops/releases audio tracks from disconnected media streams (e.g. microphone)
   */
  disconnectInput(e, n) {
    e ? Array.isArray(e) || (e = [e]) : e = Array.from(this._sources);
    for (const a of e) {
      const i = this._sources.indexOf(a);
      if (n && a.mediaStream)
        for (const r of a.mediaStream.getAudioTracks())
          r.stop();
      i >= 0 && (a.disconnect(this._input), this._sources.splice(i, 1));
    }
  }
  /**
   * Disconnects the analyzer output from other audio nodes
   *
   * @param [{object}] a connected AudioNode object; if undefined, all connected nodes are disconnected
   */
  disconnectOutput(e) {
    if (!(e && !this._outNodes.includes(e)) && (this._output.disconnect(e), this._outNodes = e ? this._outNodes.filter((n) => n !== e) : [], this._outNodes.length == 0))
      for (const n of [0, 1])
        this._analyzer[n].disconnect();
  }
  /**
   * Returns analyzer bars data
      *
   * @returns {array}
   */
  getBars() {
    return Array.from(this._bars, ({ posX: e, freq: n, freqLo: a, freqHi: i, hold: r, peak: s, value: l }) => ({ posX: e, freq: n, freqLo: a, freqHi: i, hold: r, peak: s, value: l }));
  }
  /**
   * Returns the energy of a frequency, or average energy of a range of frequencies
   *
   * @param [{number|string}] single or initial frequency (Hz), or preset name; if undefined, returns the overall energy
   * @param [{number}] ending frequency (Hz)
   * @returns {number|null} energy value (0 to 1) or null, if the specified preset is unknown
   */
  getEnergy(e, n) {
    if (e === void 0)
      return this._energy.val;
    if (e != +e) {
      if (e == "peak")
        return this._energy.peak;
      const l = {
        bass: [20, 250],
        lowMid: [250, 500],
        mid: [500, 2e3],
        highMid: [2e3, 4e3],
        treble: [4e3, 16e3]
      };
      if (!l[e])
        return null;
      [e, n] = l[e];
    }
    const a = this._freqToBin(e), i = n ? this._freqToBin(n) : a, r = this._chLayout == Se ? 1 : 2;
    let s = 0;
    for (let l = 0; l < r; l++)
      for (let d = a; d <= i; d++)
        s += this._normalizedB(this._fftData[l][d]);
    return s / (i - a + 1) / r;
  }
  /**
   * Returns current analyzer settings in object format
   *
   * @param [{string|array}] a property name or an array of property names to not include in the returned object
   * @returns {object} Options object
   */
  getOptions(e) {
    Array.isArray(e) || (e = [e]);
    let n = {};
    for (const a of Object.keys(tt))
      e.includes(a) || (a == "gradient" && this.gradientLeft != this.gradientRight ? (n.gradientLeft = this.gradientLeft, n.gradientRight = this.gradientRight) : a != "start" && (n[a] = this[a]));
    return n;
  }
  /**
   * Registers a custom gradient
   *
   * @param {string} name
   * @param {object} options
   */
  registerGradient(e, n) {
    if (typeof e != "string" || e.trim().length == 0)
      throw new me(ii);
    if (typeof n != "object")
      throw new me(ri);
    const { colorStops: a } = n;
    if (!Array.isArray(a) || !a.length)
      throw new me(si);
    const i = a.length, r = (s) => +s != s || s < 0 || s > 1;
    a.forEach((s, l) => {
      const d = l / Math.max(1, i - 1);
      typeof s != "object" ? a[l] = { pos: d, color: s } : r(s.pos) && (s.pos = d), r(s.level) && (a[l].level = 1 - l / i);
    }), a.sort((s, l) => s.level < l.level ? 1 : s.level > l.level ? -1 : 0), a[0].level = 1, this._gradients[e] = {
      bgColor: n.bgColor || Xa,
      dir: n.dir,
      colorStops: a
    }, this._selectedGrads.includes(e) && this._makeGrad();
  }
  /**
   * Set dimensions of analyzer's canvas
   *
   * @param {number} w width in pixels
   * @param {number} h height in pixels
   */
  setCanvasSize(e, n) {
    this._width = e, this._height = n, this._setCanvas(Nt);
  }
  /**
   * Set desired frequency range
   *
   * @param {number} min lowest frequency represented in the x-axis
   * @param {number} max highest frequency represented in the x-axis
   */
  setFreqRange(e, n) {
    if (e < 1 || n < 1)
      throw new me(zt);
    this._minFreq = Math.min(e, n), this.maxFreq = Math.max(e, n);
  }
  /**
   * Set custom parameters for LED effect
   * If called with no arguments or if any property is invalid, clears any previous custom parameters
   *
   * @param {object} [params]
   */
  setLedParams(e) {
    let n, a, i;
    e && (n = e.maxLeds | 0, // ensure integer
    a = +e.spaceV, i = +e.spaceH), this._ledParams = n > 0 && a > 0 && i >= 0 ? [n, a, i] : void 0, this._calcBars();
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
  setSensitivity(e, n) {
    for (const a of [0, 1])
      this._analyzer[a].minDecibels = Math.min(e, n), this._analyzer[a].maxDecibels = Math.max(e, n);
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
    const n = this.isOn;
    return e === void 0 && (e = !n), n && !e ? (cancelAnimationFrame(this._runId), this._runId = 0) : !n && e && !this._destroyed && (this._frames = 0, this._time = performance.now(), this._runId = requestAnimationFrame((a) => this._draw(a))), this.isOn;
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
    const { _ansiBands: n, _barSpace: a, canvas: i, _chLayout: r, _maxFreq: s, _minFreq: l, _mirror: d, _mode: u, _radial: _, _radialInvert: g, _reflexRatio: c } = this, p = i.width >> 1, v = i.height >> 1, A = r == Ae && !_, C = r == dt, o = u % 10 != 0, y = o && this._frequencyScale == Ye, k = this._showLeds && o && !_, x = this._lumiBars && o && !_, q = this._alphaBars && !x && u != $e, D = this._outlineBars && o && !x && !k, E = this._roundBars && o && !x && !k, z = r != Ae || c > 0 && !x, F = i.height - (A && !k ? 0.5 : 0) >> A, O = F * (x || _ ? 1 : 1 - c) | 0, h = i.width - p * (C || d != 0), re = A ? i.height - F * 2 : 0, _e = p * (d == -1 && !C && !_);
    let de = Math.min(i.width, i.height) * 0.375 * (r == Ae ? 1 : this._radius) | 0, U = Math.min(p, v);
    g && r != Ae && ([de, U] = [U, de]);
    const W = (S) => e.push({ ...S, peak: [0, 0], hold: [0], alpha: [0], value: [0] }), G = (S) => {
      const P = this._freqToBin(S, "floor"), R = this._binToFreq(P), T = this._binToFreq(P + 1), j = Math.log2(S / R) / Math.log2(T / R);
      return [P, j];
    };
    let N, Z, M;
    if (y) {
      const S = ($, Q, X) => +$.toPrecision(X ? Math.max(Q, 1 + Math.log10($) | 0) : Q), P = ($) => {
        const Q = [1, 1.12, 1.25, 1.4, 1.6, 1.8, 2, 2.24, 2.5, 2.8, 3.15, 3.55, 4, 4.5, 5, 5.6, 6.3, 7.1, 8, 9, 10], X = Math.log10($) | 0, xe = $ / 10 ** X;
        let be = 1;
        for (; be < Q.length && xe > Q[be]; )
          be++;
        return xe - Q[be - 1] < Q[be] - xe && be--, (Q[be] * 10 ** (X + 5) | 0) / 1e5;
      }, R = [0, 24, 12, 8, 6, 4, 3, 2, 1][u], T = n ? 10 ** (3 / (R * 10)) : 2 ** (1 / R), j = T ** 0.5;
      let ie = n ? 7.94328235 / (R % 2 ? 1 : j) : kn;
      do {
        let $ = ie;
        const Q = S($ / j, 4, !0), X = S($ * j, 4, !0), [xe, be] = G(Q), [Ke, ye] = G(X);
        n ? $ = R < 4 ? P($) : S($, $.toString()[0] < 5 ? 3 : 2) : $ = S($, 4, !0), $ >= l && W({ posX: 0, freq: $, freqLo: Q, freqHi: X, binLo: xe, binHi: Ke, ratioLo: be, ratioHi: ye }), ie *= T;
      } while (ie <= s);
      N = h / e.length, e.forEach(($, Q) => $.posX = _e + Q * N);
      const K = e[0], ce = e[e.length - 1];
      Z = this._freqScaling(K.freqLo), M = h / (this._freqScaling(ce.freqHi) - Z), K.freqLo < l && (K.freqLo = l, [K.binLo, K.ratioLo] = G(l)), ce.freqHi > s && (ce.freqHi = s, [ce.binHi, ce.ratioHi] = G(s));
    } else if (o) {
      const S = [0, 24, 12, 8, 6, 4, 3, 2, 1][u] * 10, P = (R) => {
        switch (this._frequencyScale) {
          case Ft:
            return 1960 / (26.81 / (R + 0.53) - 1);
          case Ot:
            return 700 * (2 ** R - 1);
          case et:
            return R;
        }
      };
      N = h / S, Z = this._freqScaling(l), M = h / (this._freqScaling(s) - Z);
      for (let R = 0, T = 0; R < S; R++, T += N) {
        const j = P(Z + T / M), ie = P(Z + (T + N / 2) / M), K = P(Z + (T + N) / M), [ce, $] = G(j), [Q, X] = G(K);
        W({ posX: _e + T, freq: ie, freqLo: j, freqHi: K, binLo: ce, binHi: Q, ratioLo: $, ratioHi: X });
      }
    } else {
      N = 1, Z = this._freqScaling(l), M = h / (this._freqScaling(s) - Z);
      const S = this._freqToBin(l, "floor"), P = this._freqToBin(s);
      let R = -999;
      for (let T = S; T <= P; T++) {
        const j = this._binToFreq(T), ie = _e + Math.round(M * (this._freqScaling(j) - Z));
        if (ie > R)
          W({ posX: ie, freq: j, freqLo: j, freqHi: j, binLo: T, binHi: T, ratioLo: 0, ratioHi: 0 }), R = ie;
        else if (e.length) {
          const K = e[e.length - 1];
          K.binHi = T, K.freqHi = j, K.freq = (K.freqLo * j) ** 0.5;
        }
      }
    }
    let oe = 0, ee = 0;
    if (k) {
      const S = this._pixelRatio / (window.devicePixelRatio > 1 && window.screen.height <= 540 ? 2 : 1), P = [
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
      ], R = this._ledParams, [T, j, ie] = R || P[u];
      let K, ce = O;
      if (R) {
        const $ = 2 * S;
        let Q;
        K = T + 1;
        do
          K--, Q = ce / K / (1 + j), ee = Q * j;
        while ((Q < $ || ee < $) && K > 1);
      } else {
        const $ = 540 / j;
        ee = Math.min(j * S, Math.max(2, ce / $ + 0.1 | 0));
      }
      z && (ce += ee), R || (K = Math.min(T, ce / (ee * 2) | 0)), oe = ie >= 1 ? ie : N * ie, this._leds = [
        K,
        oe,
        ee,
        ce / K - ee
        // ledHeight
      ];
    }
    const f = Math.min(N - 1, a * (a > 0 && a < 1 ? N : 1));
    o && (N -= Math.max(k ? oe : 0, f)), e.forEach((S, P) => {
      let R = S.posX, T = N;
      o && (a == 0 && !k ? (R |= 0, T |= 0, P > 0 && R > e[P - 1].posX + e[P - 1].width && (R--, T++)) : R += Math.max(k ? oe : 0, f) / 2, S.posX = R), S.barCenter = R + (N == 1 ? 0 : T / 2), S.width = T;
    });
    const w = [];
    for (const S of [0, 1]) {
      const P = r == Ae ? (F + re) * S : 0, R = P + F, T = P + O - (!k || z ? 0 : ee);
      w.push({ channelTop: P, channelBottom: R, analyzerBottom: T });
    }
    this._aux = { analyzerHeight: O, analyzerWidth: h, centerX: p, centerY: v, channelCoords: w, channelHeight: F, channelGap: re, initialX: _e, innerRadius: de, outerRadius: U, scaleMin: Z, unitWidth: M }, this._flg = { isAlpha: q, isBands: o, isLeds: k, isLumi: x, isOctaves: y, isOutline: D, isRound: E, noLedGap: z }, this._createScales();
  }
  /**
   * Generate the X-axis and radial scales in auxiliary canvases
   */
  _createScales() {
    if (!this._ready)
      return;
    const { analyzerWidth: e, initialX: n, innerRadius: a, scaleMin: i, unitWidth: r } = this._aux, { canvas: s, _frequencyScale: l, _mirror: d, _noteLabels: u, _radial: _, _scaleX: g, _scaleR: c } = this, p = g.canvas, v = c.canvas, A = [], C = this._chLayout == dt, o = this._chLayout == Ae, y = Math.min(s.width, s.height), k = ["C", , "D", , "E", "F", , "G", , "A", , "B"], x = y / 34 | 0, q = p.height >> 1, D = x >> 1, E = q * (u ? 0.7 : 1.5), z = D * (u ? 1 : 2), F = 2 ** (1 / 12);
    if (!u && (this._ansiBands || l != Ye))
      A.push(16, 31.5, 63, 125, 250, 500, 1e3, 2e3, 4e3), l == et ? A.push(6e3, 8e3, 1e4, 12e3, 14e3, 16e3, 18e3, 2e4, 22e3) : A.push(8e3, 16e3);
    else {
      let U = kn;
      for (let W = -1; W < 11; W++)
        for (let G = 0; G < 12; G++) {
          if (U >= this._minFreq && U <= this._maxFreq) {
            const N = k[G], Z = N == "C";
            (N && u && !d && !C || Z) && A.push(u ? [U, N + (Z ? W : "")] : U);
          }
          U *= F;
        }
    }
    v.width = v.height = Math.max(y * 0.15, (a << 1) + o * x);
    const O = v.width >> 1, h = O - x * 0.7, re = (U, W) => {
      const G = We * (U / s.width), N = G - It, Z = h * Math.cos(N), M = h * Math.sin(N);
      c.save(), c.translate(O + Z, O + M), c.rotate(G), c.fillText(W, 0, 0), c.restore();
    };
    p.width |= 0, g.fillStyle = c.strokeStyle = Ya, g.fillRect(0, 0, p.width, p.height), c.arc(O, O, O - x / 2, 0, We), c.lineWidth = x, c.stroke(), g.fillStyle = c.fillStyle = In, g.font = `${q}px ${ht}`, c.font = `${D}px ${ht}`, g.textAlign = c.textAlign = "center";
    let _e = -E / 4, de = -z;
    for (const U of A) {
      const [W, G] = Array.isArray(U) ? U : [U, U < 1e3 ? U | 0 : `${(U / 100 | 0) / 10}k`], N = r * (this._freqScaling(W) - i), Z = p.height * 0.75, M = G[0] == "C", oe = q * (u && !d && !C ? M ? 1.2 : 0.6 : 3);
      if (g.fillStyle = c.fillStyle = M && !d && !C ? Ka : In, u) {
        const ee = l == Ye, f = l == et;
        let w = ["C"];
        if ((ee || W > 2e3 || !f && W > 250 || (!_ || o) && (!f && W > 125 || W > 1e3)) && w.push("G"), (ee || W > 4e3 || !f && W > 500 || (!_ || o) && (!f && W > 250 || W > 2e3)) && w.push("E"), (f && W > 4e3 || (!_ || o) && (ee || W > 2e3 || !f && W > 500)) && w.push("D", "F", "A", "B"), !w.includes(G[0]))
          continue;
      }
      N >= _e + E / 2 && N <= e && (g.fillText(G, C && d == -1 ? e - N : n + N, Z, oe), (C || d && (N > E || d == 1)) && g.fillText(G, C && d != 1 ? e + N : (n || s.width) - N, Z, oe), _e = N + Math.min(oe, g.measureText(G).width) / 2), N >= de + z && N < e - z && (re(C && d == 1 ? e - N : N, G), (C || d && (N > z || d == 1)) && re(C && d != -1 ? e + N : -N, G), de = N);
    }
  }
  /**
   * Redraw the canvas
   * this is called 60 times per second by requestAnimationFrame()
   */
  _draw(e) {
    this._runId = requestAnimationFrame((b) => this._draw(b));
    const n = e - this._time, a = e - this._last, i = this._maxFPS ? 975 / this._maxFPS : 0;
    if (a < i)
      return;
    this._last = e - (i ? a % i : 0), this._frames++, n >= 1e3 && (this._fps = this._frames / n * 1e3, this._frames = 0, this._time = e);
    const {
      isAlpha: r,
      isBands: s,
      isLeds: l,
      isLumi: d,
      isOctaves: u,
      isOutline: _,
      isRound: g,
      noLedGap: c
    } = this._flg, {
      analyzerHeight: p,
      centerX: v,
      centerY: A,
      channelCoords: C,
      channelHeight: o,
      channelGap: y,
      initialX: k,
      innerRadius: x,
      outerRadius: q
    } = this._aux, {
      _bars: D,
      canvas: E,
      _canvasGradients: z,
      _chLayout: F,
      _colorMode: O,
      _ctx: h,
      _energy: re,
      _fadePeaks: _e,
      fillAlpha: de,
      _fps: U,
      _linearAmplitude: W,
      _lineWidth: G,
      maxDecibels: N,
      minDecibels: Z,
      _mirror: M,
      _mode: oe,
      overlay: ee,
      _radial: f,
      showBgColor: w,
      showPeaks: S,
      useCanvas: P,
      _weightingFilter: R
    } = this, T = this._scaleX.canvas, j = this._scaleR.canvas, ie = U * this._peakFadeTime / 1e3, K = U ** 2, ce = this._gravity * 1e3, $ = U * this._peakHoldTime / 1e3, Q = F == wn, X = F == dt, xe = F == Ae, be = F == Se, Ke = l && this._trueLeds && O == mt, ye = f ? E.width : this._aux.analyzerWidth, Lt = k + ye, Qe = S && this._peakLine && oe == $e, Te = f ? q - x : p, ga = Te / this._pixelRatio, [ma, Li, Je, Rt] = this._leds || [];
    re.val > 0 && U > 0 && (this._spinAngle += this._spinSpeed * We / 60 / U);
    const ba = (b) => {
      if (this._reflexRatio > 0 && !d && !f) {
        let H, ke;
        this.reflexFit || xe ? (H = xe && b == 0 ? o + y : 0, ke = o - p) : (H = E.height - p * 2, ke = p), h.save(), h.globalAlpha = this.reflexAlpha, this.reflexBright != 1 && (h.filter = `brightness(${this.reflexBright})`), h.setTransform(1, 0, 0, -1, 0, E.height), h.drawImage(E, 0, C[b].channelTop, E.width, p, 0, H, E.width, ke), h.restore();
      }
    }, va = () => {
      this.showScaleX && (f ? (h.save(), h.translate(v, A), this._spinSpeed && h.rotate(this._spinAngle + It), h.drawImage(j, -j.width >> 1, -j.width >> 1), h.restore()) : h.drawImage(T, 0, E.height - T.height));
    }, xa = (b) => {
      const H = b ** 2, ke = 424.36, le = 11599.29, rt = 25122.25, Me = 544496.41, Le = 148693636, Ne = (Fe) => 20 * Math.log10(Fe);
      switch (R) {
        case Cn:
          const Fe = Le * H ** 2 / ((H + ke) * Math.sqrt((H + le) * (H + Me)) * (H + Le));
          return 2 + Ne(Fe);
        case En:
          const Oe = Le * H * b / ((H + ke) * Math.sqrt(H + rt) * (H + Le));
          return 0.17 + Ne(Oe);
        case Tn:
          const Et = Le * H / ((H + ke) * (H + Le));
          return 0.06 + Ne(Et);
        case Mn:
          const st = ((103791848e-2 - H) ** 2 + 108076816e-2 * H) / ((9837328 - H) ** 2 + 11723776 * H), Tt = b / 68966888496476e-18 * Math.sqrt(st / ((H + 79919.29) * (H + 1345600)));
          return Ne(Tt);
        case Bn:
          const ot = -4737338981378384e-39 * b ** 6 + 2043828333606125e-30 * b ** 4 - 1363894795463638e-22 * H + 1, ze = 1306612257412824e-34 * b ** 5 - 2118150887518656e-26 * b ** 3 + 5559488023498642e-19 * b, pe = 1246332637532143e-19 * b / Math.hypot(ot, ze);
          return 18.2 + Ne(pe);
      }
      return 0;
    }, St = (b, H, ke) => {
      h.beginPath(), h.moveTo(b, H), h.lineTo(b, ke), h.stroke();
    }, Ct = (b) => {
      if (b && G) {
        const H = h.globalAlpha;
        h.globalAlpha = 1, h.stroke(), h.globalAlpha = H;
      }
    }, Ie = (b) => Math.max(0, (b * ma | 0) * (Rt + Je) - Je), ya = (b) => {
      re.val = b, re.peak > 0 && (re.hold--, re.hold < 0 && (re.peak += re.hold * ce / K / E.height * this._pixelRatio)), b >= re.peak && (re.peak = b, re.hold = $);
    };
    ee && h.clearRect(0, 0, E.width, E.height);
    let nn = 0;
    const an = D.length, rn = be ? 1 : 2;
    for (let b = 0; b < rn; b++) {
      const { channelTop: H, channelBottom: ke, analyzerBottom: le } = C[b], rt = this._gradients[this._selectedGrads[b]], Me = rt.colorStops, Le = Me.length, Ne = !w || l && !ee ? "#000" : rt.bgColor, Fe = xe && f && b ? -1 : 1, Oe = !b && M == -1 || b && M == 1, Et = !X || b && M != 1 ? 0 : ye >> (b || !Oe), st = X && Oe ? -1 : 1, Tt = () => {
        const L = T.height, B = L >> 1, I = W ? 100 : N, J = W ? 0 : Z, ne = W ? 20 : 5, fe = p / (I - J), we = M != -1 && (!X || b == 0 || M == 1), Be = M != 1 && (!X || b != M);
        h.save(), h.fillStyle = Nn, h.font = `${B}px ${ht}`, h.textAlign = "right", h.lineWidth = 1;
        for (let De = I; De > J; De -= ne) {
          const Xe = H + (I - De) * fe, se = De % 2 == 0 | 0;
          if (se) {
            const he = Xe + B * (Xe == H ? 0.8 : 0.35);
            we && h.fillText(De, L * 0.85, he), Be && h.fillText(De, (X ? ye : E.width) - L * 0.1, he), h.strokeStyle = Nn, h.setLineDash([2, 4]), h.lineDashOffset = 0;
          } else
            h.strokeStyle = Qa, h.setLineDash([2, 8]), h.lineDashOffset = 1;
          h.beginPath(), h.moveTo(k + L * se * we, ~~Xe + 0.5), h.lineTo(Lt - L * se * Be, ~~Xe + 0.5), h.stroke();
        }
        h.restore();
      }, ot = (L, B) => {
        const I = ve[L] + (L < ve.length - 1 ? (ve[L + 1] - ve[L]) * B : 0);
        return isNaN(I) ? -1 / 0 : I;
      }, ze = (L, B = st) => B * We * ((L + Et) / E.width) + this._spinAngle, pe = (L, B, I) => {
        const J = x + B * Fe, ne = ze(L, I);
        return [v + J * Math.cos(ne), A + J * Math.sin(ne)];
      }, Mt = (L, B, I, J, ne) => {
        h.beginPath();
        for (const fe of M && !X ? [1, -1] : [st]) {
          const [we, Be] = g ? [ze(L, fe), ze(L + I, fe)] : [];
          h.moveTo(...pe(L, B, fe)), h.lineTo(...pe(L, B + J, fe)), g ? h.arc(v, A, x + (B + J) * Fe, we, Be, fe != 1) : h.lineTo(...pe(L + I, B + J, fe)), h.lineTo(...pe(L + I, B, fe)), g && !ne && h.arc(v, A, x + B * Fe, Be, we, fe == 1);
        }
        Ct(ne), h.fill();
      }, Bt = (L = 0, B = 0) => {
        let I;
        if (O == mt && !Ke || oe == $e)
          I = z[b];
        else {
          const J = O == An ? B % Le : Me.findLastIndex((ne) => l ? Ie(L) <= Ie(ne.level) : L <= ne.level);
          I = Me[J].color;
        }
        h.fillStyle = h.strokeStyle = I;
      };
      if (P) {
        if (X && !f) {
          const L = ye * (b + Oe), B = Oe ? -1 : 1;
          h.setTransform(B, 0, 0, 1, L, 0);
        }
        if ((!ee || w) && (ee && (h.globalAlpha = this.bgAlpha), h.fillStyle = Ne, (b == 0 || !f && !Q) && h.fillRect(k, H - y, ye, (ee && this.reflexAlpha == 1 ? p : o) + y), h.globalAlpha = 1), this.showScaleY && !d && !f && (b == 0 || !Q) && Tt(), l ? (h.setLineDash([Rt, Je]), h.lineWidth = D[0].width) : h.lineWidth = _ ? Math.min(G, D[0].width / 2) : G, h.save(), !f) {
          const L = new Path2D();
          L.rect(0, H, E.width, p), h.clip(L);
        }
      }
      let ve = this._fftData[b];
      this._analyzer[b].getFloatFrequencyData(ve), R && (ve = ve.map((L, B) => L + xa(this._binToFreq(B)))), h.beginPath();
      let Ze = [];
      for (let L = 0; L < an; L++) {
        const B = D[L], { posX: I, barCenter: J, width: ne, freq: fe, binLo: we, binHi: Be, ratioLo: De, ratioHi: Xe } = B;
        let se = Math.max(ot(we, De), ot(Be, Xe));
        for (let Y = we + 1; Y < Be; Y++)
          ve[Y] > se && (se = ve[Y]);
        if (se = this._normalizedB(se), B.value[b] = se, nn += se, B.peak[b] > 0 && B.alpha[b] > 0 && (B.hold[b]--, B.hold[b] < 0)) {
          if (_e && !Qe) {
            const Y = !r || _ && G > 0 ? 1 : r ? B.peak[b] : de;
            B.alpha[b] = Y * (1 + B.hold[b] / ie);
          } else
            B.peak[b] += B.hold[b] * ce / K / Math.abs(ga);
          B.alpha[b] <= 0 && (B.peak[b] = 0);
        }
        if (se >= B.peak[b] && (B.peak[b] = se, B.hold[b] = $, B.alpha[b] = !r || _ && G > 0 ? 1 : r ? se : de), !P)
          continue;
        h.globalAlpha = d || r ? se : _ ? de : 1, Bt(se, L);
        const he = d ? Te : l ? Ie(se) : se * Te | 0;
        if (oe == $e) {
          const Y = L ? 0 : (this._normalizedB(ve[D[1].binLo]) * Te + he) / 2;
          if (f) {
            if (L == 0 && (X && h.moveTo(...pe(0, 0)), h.lineTo(...pe(0, I < 0 ? Y : he))), I >= 0) {
              const ge = [I, he];
              h.lineTo(...pe(...ge)), Ze.push(ge);
            }
          } else {
            if (L == 0)
              if (M == -1 && !X)
                h.moveTo(k, le - (I < k ? Y : he));
              else {
                const ge = we ? this._normalizedB(ve[we - 1]) * Te : he;
                h.moveTo(k - G, le - ge);
              }
            (X || M != -1 || I >= k) && h.lineTo(I, le - he);
          }
        } else if (l) {
          if (w && !ee && (b == 0 || !Q)) {
            const Y = h.globalAlpha;
            h.strokeStyle = Wa, h.globalAlpha = 1, St(J, H, le), h.strokeStyle = h.fillStyle, h.globalAlpha = Y;
          }
          if (Ke) {
            const Y = d ? 0 : Me.findLastIndex((qe) => Ie(se) <= Ie(qe.level));
            let ge = le;
            for (let qe = Le - 1; qe >= Y; qe--) {
              h.strokeStyle = Me[qe].color;
              let on = le - (qe == Y ? he : Ie(Me[qe].level));
              St(J, ge, on), ge = on - Je;
            }
          } else
            St(J, le, le - he);
        } else if (I >= k)
          if (f)
            Mt(I, 0, ne, he, _);
          else if (g) {
            const Y = ne / 2, ge = le + Y;
            h.beginPath(), h.moveTo(I, ge), h.lineTo(I, ge - he), h.arc(J, ge - he, Y, tn, We), h.lineTo(I + ne, ge), Ct(_), h.fill();
          } else {
            const Y = _ ? h.lineWidth : 0;
            h.beginPath(), h.rect(I, le + Y, ne, -he - Y), Ct(_), h.fill();
          }
        const Ue = B.peak[b], sn = B.alpha[b];
        if (Ue > 0 && sn > 0 && S && !Qe && !d && I >= k && I < Lt) {
          if (_e ? h.globalAlpha = sn : _ && G > 0 ? h.globalAlpha = 1 : r && (h.globalAlpha = Ue), (O == Ln || Ke) && Bt(Ue), l) {
            const Y = Ie(Ue);
            Y >= Je && h.fillRect(I, le - Y, ne, Rt);
          } else if (!f)
            h.fillRect(I, le - Ue * Te, ne, 2);
          else if (oe != $e) {
            const Y = Ue * Te;
            Mt(I, Y, ne, !this._radialInvert || xe || Y + x >= 2 ? -2 : 2);
          }
        }
      }
      if (P) {
        if (h.globalAlpha = 1, oe == $e) {
          if (Bt(), f && !X) {
            if (M) {
              let L;
              for (; L = Ze.pop(); )
                h.lineTo(...pe(...L, -1));
            }
            h.closePath();
          }
          if (G > 0 && h.stroke(), de > 0) {
            if (f) {
              const L = X ? ze(ye >> 1) : 0, B = X ? ze(ye) : We;
              h.moveTo(...pe(X ? ye >> 1 : 0, 0)), h.arc(v, A, x, L, B, X ? !Oe : !0);
            } else
              h.lineTo(Lt, le), h.lineTo(k, le);
            h.globalAlpha = de, h.fill(), h.globalAlpha = 1;
          }
          if ((Qe || f && S) && (Ze = [], h.beginPath(), D.forEach((L, B) => {
            let I = L.posX, J = L.peak[b], ne = B ? "lineTo" : "moveTo";
            if (f && I < 0) {
              const fe = D[B + 1];
              J = oi(I, J, fe.posX, fe.peak[b], 0), I = 0;
            }
            J *= Te, Qe ? (h[ne](...f ? pe(I, J) : [I, le - J]), f && M && !X && Ze.push([I, J])) : L.peak[b] > 0 && (_e && (h.globalAlpha = L.alpha[b]), Mt(I, J, 1, -2));
          }), Qe)) {
            let L;
            for (; L = Ze.pop(); )
              h.lineTo(...pe(...L, -1));
            h.lineWidth = 1, h.stroke();
          }
        }
        h.restore(), X && !f && h.setTransform(1, 0, 0, 1, 0, 0), (!X && !Q || b) && ba(b);
      }
    }
    if (ya(nn / (an << rn - 1)), P && (M && !f && !X && (h.setTransform(-1, 0, 0, 1, E.width - k, 0), h.drawImage(E, k, 0, v, E.height, 0, 0, v, E.height), h.setTransform(1, 0, 0, 1, 0, 0)), h.setLineDash([]), va()), this.showFPS) {
      const b = T.height;
      h.font = `bold ${b}px ${ht}`, h.fillStyle = Ua, h.textAlign = "right", h.fillText(Math.round(U), E.width - b, b * 2);
    }
    this.onCanvasDraw && (h.save(), h.fillStyle = h.strokeStyle = z[0], this.onCanvasDraw(this, { timestamp: e, canvasGradients: z }), h.restore());
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
  _freqToBin(e, n = "round") {
    const a = this._analyzer[0].frequencyBinCount - 1, i = Math[n](e * this.fftSize / this.audioCtx.sampleRate);
    return i < a ? i : a;
  }
  /**
   * Generate currently selected gradient
   */
  _makeGrad() {
    if (!this._ready)
      return;
    const { canvas: e, _ctx: n, _radial: a, _reflexRatio: i } = this, { analyzerWidth: r, centerX: s, centerY: l, initialX: d, innerRadius: u, outerRadius: _ } = this._aux, { isLumi: g } = this._flg, c = this._chLayout == Ae, p = 1 - i, v = g ? e.height : e.height * (1 - i * !c) | 0;
    for (const A of [0, 1]) {
      const C = this._gradients[this._selectedGrads[A]], o = C.colorStops, y = C.dir == "h";
      let k;
      if (a ? k = n.createRadialGradient(s, l, _, s, l, u - (_ - u) * c) : k = n.createLinearGradient(...y ? [d, 0, d + r, 0] : [0, 0, 0, v]), o) {
        const x = c && !this._splitGradient && (!y || a);
        for (let q = 0; q < 1 + x; q++) {
          const D = o.length - 1;
          o.forEach((E, z) => {
            let F = E.pos;
            if (x && (F /= 2), c && !g && !a && !y && (F *= p, !x && F > 0.5 * p && (F += 0.5 * i)), q == 1)
              if (a || g) {
                const O = D - z;
                E = o[O], F = 1 - E.pos / 2;
              } else
                z == 0 && F > 0 && k.addColorStop(0.5, E.color), F += 0.5;
            k.addColorStop(F, E.color), c && z == D && F < 0.5 && k.addColorStop(0.5, E.color);
          });
        }
      }
      this._canvasGradients[A] = k;
    }
  }
  /**
   * Normalize a dB value in the [0;1] range
   */
  _normalizedB(e) {
    const n = this._linearAmplitude, a = n ? 1 / this._linearBoost : 1, i = (d, u, _) => d <= u ? u : d >= _ ? _ : d, r = (d) => 10 ** (d / 20);
    let s = this.maxDecibels, l = this.minDecibels;
    return n && (s = r(s), l = r(l), e = r(e) ** a), i((e - l) / (s - l) ** a, 0, 1);
  }
  /**
   * Internal function to change canvas dimensions on demand
   */
  _setCanvas(e) {
    if (!this._ready)
      return;
    const { canvas: n, _ctx: a } = this, i = this._scaleX.canvas, r = window.devicePixelRatio / (this._loRes + 1);
    let s = window.screen.width * r, l = window.screen.height * r;
    Math.abs(window.orientation) == 90 && s < l && ([s, l] = [l, s]);
    const d = this.isFullscreen, u = d && this._fsEl == n, _ = u ? s : (this._width || this._container.clientWidth || this._defaultWidth) * r | 0, g = u ? l : (this._height || this._container.clientHeight || this._defaultHeight) * r | 0;
    this._pixelRatio = r, this._fsWidth = s, this._fsHeight = l, !(e != Dn && n.width == _ && n.height == g) && (n.width = _, n.height = g, this.overlay || (a.fillStyle = "#000", a.fillRect(0, 0, _, g)), a.lineJoin = "bevel", i.width = _, i.height = Math.max(20 * r, Math.min(_, g) / 32 | 0), this._calcBars(), this._makeGrad(), this._fsStatus !== void 0 && this._fsStatus !== d && (e = Pn), this._fsStatus = d, this.onCanvasResize && this.onCanvasResize(e, this));
  }
  /**
   * Select a gradient for one or both channels
   *
   * @param {string} name gradient name
   * @param [{number}] desired channel (0 or 1) - if empty or invalid, sets both channels
   */
  _setGradient(e, n) {
    if (!this._gradients.hasOwnProperty(e))
      throw new me(ei, e);
    [0, 1].includes(n) || (this._selectedGrads[1] = e, n = 0), this._selectedGrads[n] = e, this._makeGrad();
  }
  /**
   * Set object properties
   */
  _setProps(e, n) {
    const a = ["onCanvasDraw", "onCanvasResize"], i = ["gradientLeft", "gradientRight", "stereo"], r = Object.keys(tt).filter((s) => s != "start").concat(a, i);
    (n || e === void 0) && (e = { ...tt, ...e });
    for (const s of Object.keys(e))
      a.includes(s) && typeof e[s] != "function" ? this[s] = void 0 : r.includes(s) && (this[s] = e[s]);
    e.start !== void 0 && this.toggleAnalyzer(e.start);
  }
}
const Vt = "#c2ff3a", qt = /^#[0-9a-f]{6}$/i, ca = (t) => [1, 3, 5].map((e) => parseInt(t.slice(e, e + 2), 16)), ci = (t) => "#" + t.map((e) => Math.max(0, Math.min(255, Math.round(e))).toString(16).padStart(2, "0")).join(""), kt = (t, e) => ci(ca(t).map((n) => e > 0 ? n + (255 - n) * e : n * (1 + e))), da = (t) => [
  kt(t, 0.3),
  t,
  kt(t, -0.6)
];
let di = 0;
const ha = () => `r${++di}`, Re = (t) => typeof t == "string" ? t : "";
function qn(t, e) {
  const n = Array.isArray(t) ? t : t?.records;
  if (!Array.isArray(n)) return [];
  const a = (i) => Re(i) ? new URL(Re(i), e).href : void 0;
  return n.flatMap((i) => {
    const r = (Array.isArray(i?.tracks) ? i.tracks : []).filter((d) => Re(d?.src)).map((d, u) => ({
      title: Re(d.title) || `Track ${u + 1}`,
      src: a(d.src),
      duration: typeof d.duration == "number" ? d.duration : void 0
    }));
    if (!r.length) return [];
    const s = qt.test(Re(i.accent)) ? i.accent : Vt, l = Array.isArray(i.gradient) && i.gradient.length === 3 && i.gradient.every((d) => qt.test(Re(d)));
    return [
      {
        id: ha(),
        title: Re(i.title) || "Untitled",
        artist: Re(i.artist),
        cover: a(i.cover),
        link: a(i.link),
        accent: s,
        gradient: l ? i.gradient : da(s),
        tracks: r,
        auto: { accent: !qt.test(Re(i.accent)), gradient: !l }
      }
    ];
  });
}
async function hi(t, e) {
  const n = t.filter((i) => i.cover && i.auto && (i.auto.accent || i.auto.gradient)), a = await Promise.all(n.map((i) => e(i.cover)));
  return n.forEach((i, r) => {
    const s = a[r];
    s && (i.auto.accent && (i.accent = s.accent), i.auto.gradient && (i.gradient = i.auto.accent ? s.gradient : [s.gradient[0], i.accent, s.gradient[2]]), i.auto = void 0);
  }), a.some(Boolean);
}
const ui = {
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
function ua(t, e) {
  const n = new li(t, { ...ui, ...e }), a = (r, s) => {
    n.registerGradient(r, { bgColor: "transparent", colorStops: [...s] }), n.gradient !== r && (n.gradient = r);
  };
  let i = 0;
  return {
    setRecord: (r) => a(r.id, r.gradient),
    setMix(r) {
      const s = r.reduce((d, [, u]) => d + u, 0);
      if (s < 1e-4) return;
      const l = [0, 1, 2].map((d) => {
        const u = [0, 0, 0];
        for (const [_, g] of r) ca(_.gradient[d]).forEach((c, p) => u[p] += c * g / s);
        return `rgb(${u.map(Math.round).join(",")})`;
      });
      a("mix", l);
    },
    setActive(r) {
      window.clearTimeout(i), t.classList.toggle("is-on", r), r ? !n.isOn && n.start() : i = window.setTimeout(() => n.stop(), 1200);
    }
  };
}
const fi = (t, e) => ua(e, { source: t, mode: 3, barSpace: 0.3 }), _i = (t, e) => ua(e, { source: t, mode: 4, barSpace: 0.35, radial: !0, radius: 0.7, spinSpeed: 2 }), Hn = 200, Ht = -8, nt = 4, Gt = 27, Gn = { x: 0.98, y: 0.02 }, jn = (t, e, n, a) => Math.atan2(e - a, t - n) * 180 / Math.PI, pi = (t) => ((t + 180) % 360 + 360) % 360 - 180, gi = (t) => String(t).padStart(2, "0");
function mi(t) {
  const { side: e, engine: n, channel: a, dj: i, records: r, t: s } = t, [, l] = He(0), d = () => l((f) => f + 1), u = ue(null), _ = ue(null), g = ue(null), c = ue(null), p = ue(null), v = ue(null), A = ue(""), C = ue(t);
  C.current = t;
  const o = ue({
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
    armAngle: Ht,
    scratch: null,
    armDrag: !1,
    braking: !1,
    pitch: 0,
    // ±0.08 = ±8 %
    cues: [null, null, null],
    dropping: !1
  }).current, y = () => n.unlock().then(() => {
    !v.current && c.current && (v.current = _i(a.output, c.current));
  }), k = (f, w, S = { sec: 0 }) => {
    const P = `${f.id}/${w}`;
    Object.assign(o, { rec: f, track: w, needle: !0, braking: !1, failed: !1 });
    const R = f.tracks[w].duration;
    o.loaded === P && R ? (o.pos = "sec" in S ? S.sec : S.frac * R, a.seek(o.pos)) : (o.loading = !0, o.loaded = "", o.pos = 0, a.load(f.tracks[w].src, (T) => "sec" in S ? S.sec : S.frac * T).then((T) => {
      T !== null && (f.tracks[w].duration = T, o.loaded = P, o.loading = !1, d());
    }).catch(() => {
      Object.assign(o, { loading: !1, needle: !1, failed: !0 }), d();
    })), y(), d();
  }, x = () => {
    o.needle = !1, o.braking = !1, d();
  }, q = () => {
    if (y(), !o.rec) return r[0] && k(r[0], 0);
    o.needle && !o.braking ? o.braking = !0 : (o.needle || (o.rate = 0), o.needle = !0, o.braking = !1), d();
  }, D = (f) => {
    if (!o.rec) return;
    const w = o.track + f;
    w >= 0 && w < o.rec.tracks.length && k(o.rec, w);
  };
  Ee(() => {
    a.onPosition = (P, R) => {
      o.loading || (o.pos = P, !(!R || !o.needle || !o.rec) && (o.track + 1 < o.rec.tracks.length ? k(o.rec, o.track + 1) : (Object.assign(o, { needle: !1, track: 0, pos: 0 }), a.seek(0), d())));
    };
    let f = 0, w = performance.now();
    const S = (P) => {
      const R = Math.min(0.05, (P - w) / 1e3);
      if (w = P, o.scratch)
        P - o.scratch.t > 50 && (o.scratch.vel *= 0.6), o.rate = o.scratch.vel;
      else {
        const T = o.braking ? 0 : 1 + o.pitch;
        o.rate += (T - o.rate) * Math.min(1, R * (o.braking ? 2.2 : 5)), Math.abs(T - o.rate) < 2e-3 && (o.rate = T), o.braking && o.rate < 0.02 && x(), o.angle += o.rate * Hn * R;
      }
      if (a.setRate(o.needle && !o.loading && !o.armDrag ? o.rate : 0), !o.armDrag) {
        let T = Ht;
        if (o.needle && o.rec) {
          const j = o.rec.tracks[o.track].duration, ie = j ? Math.min(1, o.pos / j) : 0;
          T = nt + (Gt - nt) * (o.track + ie) / o.rec.tracks.length;
        }
        o.armAngle += (T - o.armAngle) * Math.min(1, R * 6);
      }
      _.current && (_.current.style.transform = `rotate(${o.angle}deg)`), g.current && (g.current.style.transform = `rotate(${o.armAngle}deg)`), f = requestAnimationFrame(S);
    };
    return f = requestAnimationFrame(S), () => cancelAnimationFrame(f);
  }, []), Ee(() => {
    const f = o.needle && !o.loading, w = o.rec && `${o.rec.id}:${o.rec.gradient.join()}`;
    o.rec && w && A.current !== w && (v.current?.setRecord(o.rec), v.current && (A.current = w)), v.current?.setActive(f), C.current.onStatus(e, { playing: f && !o.braking, rec: o.rec });
  }), Ee(() => {
    i || (o.pitch = 0, e === "b" && x());
  }, [i]), Ee(() => {
    o.rec && !r.includes(o.rec) && (Object.assign(o, { rec: null, needle: !1 }), d());
  }, [r]);
  const E = () => {
    const f = u.current.getBoundingClientRect();
    return { cx: f.left + f.width / 2, cy: f.top + f.height / 2, r: f };
  }, z = (f) => {
    y(), f.currentTarget.setPointerCapture(f.pointerId);
    const { cx: w, cy: S } = E();
    o.scratch = { last: jn(f.clientX, f.clientY, w, S), t: performance.now(), vel: 0 };
  }, F = (f) => {
    if (!o.scratch) return;
    const { cx: w, cy: S } = E(), P = jn(f.clientX, f.clientY, w, S), R = performance.now(), T = pi(P - o.scratch.last), j = Math.max(4e-3, (R - o.scratch.t) / 1e3);
    o.angle += T;
    const ie = Math.max(-4, Math.min(4, T / j / Hn));
    o.scratch.vel = o.scratch.vel * 0.5 + ie * 0.5, o.scratch.last = P, o.scratch.t = R;
  }, O = () => {
    o.scratch = null;
  }, h = (f) => {
    const { r: w } = E(), S = w.left + Gn.x * w.width, P = w.top + Gn.y * w.height, R = Math.atan2(-(f.clientX - S), f.clientY - P) * 180 / Math.PI;
    return Math.max(Ht, Math.min(Gt + 2, R));
  }, re = (f) => {
    y(), f.stopPropagation(), f.currentTarget.setPointerCapture(f.pointerId), o.armDrag = !0;
  }, _e = (f) => {
    o.armDrag && (o.armAngle = h(f));
  }, de = (f) => {
    if (!o.armDrag) return;
    o.armDrag = !1;
    const w = h(f), S = o.rec ?? r[0];
    if (w < nt - 1.5 || !S) return x();
    const P = Math.min(0.999, Math.max(0, (w - nt) / (Gt - nt))) * S.tracks.length, R = Math.floor(P);
    k(S, R, { frac: P - R });
  }, U = async (f) => {
    y();
    const w = await C.current.onFiles(f);
    w && k(w, 0);
  }, W = (f) => {
    !t.uploads || !f.dataTransfer?.files.length || (f.preventDefault(), f.stopPropagation(), o.dropping = !1, U([...f.dataTransfer.files]));
  }, G = (f) => {
    !t.uploads || !f.dataTransfer?.types.includes("Files") || (f.preventDefault(), o.dropping || (o.dropping = !0, d()));
  }, N = (f) => {
    y();
    const w = o.cues[f];
    if (w) return k(w.rec, w.track, { sec: w.pos });
    o.rec && (o.cues[f] = { rec: o.rec, track: o.track, pos: o.pos }, d());
  }, Z = (f) => {
    o.cues[f] = null, d();
  }, M = o.rec, oe = o.needle && !o.braking, ee = M && (M.link ? /* @__PURE__ */ m("a", { href: M.link, target: "_blank", rel: "noopener", children: M.artist }) : M.artist);
  return /* @__PURE__ */ m(
    "div",
    {
      class: `at-deck at-deck-${e}${o.dropping ? " is-dropping" : ""}`,
      style: M ? { "--deck-accent": M.accent } : void 0,
      onDragOver: G,
      onDragLeave: () => o.dropping && (o.dropping = !1, d()),
      onDrop: W,
      children: [
        /* @__PURE__ */ m("div", { ref: u, class: "at-deck-stage", children: [
          /* @__PURE__ */ m("div", { ref: c, class: "at-deck-ring", "aria-hidden": "true" }),
          /* @__PURE__ */ m(
            "div",
            {
              class: "at-deck-disc",
              onPointerDown: z,
              onPointerMove: F,
              onPointerUp: O,
              onPointerCancel: O,
              children: /* @__PURE__ */ m("div", { ref: _, class: "at-vinyl", children: M && /* @__PURE__ */ m("div", { class: "at-deck-label", children: M.cover ? /* @__PURE__ */ m("img", { src: M.cover, alt: "", draggable: !1 }) : /* @__PURE__ */ m("span", { children: M.title }) }) })
            },
            M?.id ?? "idle"
          ),
          !M && e === "a" && t.logo && /* @__PURE__ */ m("img", { class: "at-vinyl-logo", src: t.logo, alt: "", draggable: !1 }),
          o.dropping && /* @__PURE__ */ m("div", { class: "at-deck-drop", children: s.drop }),
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
            r.map((f) => /* @__PURE__ */ m(
              "button",
              {
                class: "at-deck-sleeve",
                style: { "--sleeve-accent": f.accent },
                "aria-pressed": M === f,
                title: `${f.artist} – ${f.title}`,
                onClick: () => k(f, 0),
                children: f.cover ? /* @__PURE__ */ m("img", { src: f.cover, alt: `${f.artist} – ${f.title}` }) : /* @__PURE__ */ m("span", { children: f.title })
              },
              f.id
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
                  onChange: (f) => {
                    const w = f.currentTarget;
                    U([...w.files ?? []]), w.value = "";
                  }
                }
              )
            ] })
          ] }),
          /* @__PURE__ */ m("div", { class: "at-deck-now", children: [
            /* @__PURE__ */ m("div", { class: "at-deck-meta", "aria-live": "polite", children: M ? /* @__PURE__ */ m(Ge, { children: [
              /* @__PURE__ */ m("span", { class: "at-deck-band", children: [
                ee,
                M.artist && " · ",
                M.title
              ] }),
              /* @__PURE__ */ m("span", { class: "at-deck-track", children: [
                gi(o.track + 1),
                " · ",
                M.tracks[o.track].title,
                o.loading && ` · ${s.loading}`,
                o.failed && ` · ${s.error}`
              ] })
            ] }) : /* @__PURE__ */ m(Ge, { children: [
              /* @__PURE__ */ m("span", { class: "at-deck-band", children: i ? `${s.deck} ${e.toUpperCase()}` : s.pick }),
              /* @__PURE__ */ m("span", { class: "at-deck-track", children: i ? s.pick : s.hint })
            ] }) }),
            /* @__PURE__ */ m("div", { class: "at-deck-controls", children: [
              /* @__PURE__ */ m("button", { class: "at-deck-btn", onClick: () => D(-1), "aria-label": s.prev, children: "⏮" }),
              /* @__PURE__ */ m("button", { class: "at-deck-btn at-deck-btn-main", onClick: q, "aria-label": oe ? s.pause : s.play, children: oe ? "❚❚" : "▶" }),
              /* @__PURE__ */ m("button", { class: "at-deck-btn", onClick: () => D(1), "aria-label": s.next, children: "⏭" })
            ] })
          ] }),
          i && /* @__PURE__ */ m("div", { class: "at-deck-dj", children: [
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
                  onInput: (f) => o.pitch = +f.currentTarget.value,
                  onDblClick: (f) => {
                    f.currentTarget.value = "0", o.pitch = 0;
                  }
                }
              )
            ] }),
            /* @__PURE__ */ m("div", { class: "at-pads", children: [
              o.cues.map((f, w) => /* @__PURE__ */ m(
                "button",
                {
                  class: "at-pad",
                  "aria-pressed": !!f,
                  title: f ? s.cueJump : s.cueSet,
                  onClick: () => N(w),
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
                    y(), o.scratch || (o.rate = -4);
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
const Yt = (t, e) => (t[e] & 127) << 21 | (t[e + 1] & 127) << 14 | (t[e + 2] & 127) << 7 | t[e + 3] & 127, Xn = (t, e) => (t[e] << 24 | t[e + 1] << 16 | t[e + 2] << 8 | t[e + 3]) >>> 0;
function fa(t) {
  return t.length < 10 || t[0] !== 73 || t[1] !== 68 || t[2] !== 51 ? 0 : 10 + Yt(t, 6);
}
const Un = ["latin1", "utf-16", "utf-16be", "utf-8"].map((t) => new TextDecoder(t));
function Ve(t, e, n, a) {
  const i = a === 1 || a === 2;
  let r = e;
  if (i) for (; r + 1 < n && !(t[r] === 0 && t[r + 1] === 0); ) r += 2;
  else for (; r < n && t[r] !== 0; ) r++;
  return [(Un[a] ? Un[a].decode(t.subarray(e, r)) : "").replace(/^﻿/, "").trim(), Math.min(n, r + (i ? 2 : 1))];
}
function bi(t) {
  const e = new Uint8Array(t), n = fa(e);
  if (!n) return {};
  const a = e[3];
  if (a !== 3 && a !== 4) return {};
  const i = Math.min(n, e.length);
  let r = 10;
  e[5] & 64 && (r += a === 4 ? Yt(e, 10) : Xn(e, 10) + 4);
  const s = {};
  for (; r + 10 <= i; ) {
    const l = String.fromCharCode(e[r], e[r + 1], e[r + 2], e[r + 3]);
    if (!/^[A-Z0-9]{4}$/.test(l)) break;
    const d = a === 4 ? Yt(e, r + 4) : Xn(e, r + 4), u = r + 10, _ = Math.min(i, u + d);
    if (r = _, d <= 0) continue;
    const g = e[u];
    if (l === "TIT2") s.title = Ve(e, u + 1, _, g)[0];
    else if (l === "TPE1") s.artist = Ve(e, u + 1, _, g)[0];
    else if (l === "TALB") s.album = Ve(e, u + 1, _, g)[0];
    else if (l === "TRCK") s.track = parseInt(Ve(e, u + 1, _, g)[0], 10) || void 0;
    else if (l === "APIC" && !s.picture) {
      const [c, p] = Ve(e, u + 1, _, 0), [, v] = Ve(e, p + 1, _, g);
      s.picture = { mime: c.includes("/") ? c : `image/${c || "jpeg"}`, data: e.slice(v, _) };
    }
  }
  return s;
}
const jt = (t) => "#" + t.map((e) => Math.round(Math.max(0, Math.min(255, e))).toString(16).padStart(2, "0")).join("");
function _a(t, e, n) {
  const a = Math.max(t, e, n), i = Math.min(t, e, n), r = a - i;
  let s = 0;
  return r && (s = a === t ? (e - n) / r % 6 : a === e ? (n - t) / r + 2 : (t - e) / r + 4), { h: (s * 60 + 360) % 360, s: a ? r / a : 0, v: a / 255 };
}
function Xt(t, e, n = 0.8) {
  const { h: a, s: i, v: r } = _a(t[0], t[1], t[2]), s = Math.max(i, n), l = Math.max(r, e / 255), d = (u) => {
    const _ = (u + a / 60) % 6;
    return 255 * l * (1 - s * Math.max(0, Math.min(_, 4 - _, 1)));
  };
  return [d(5), d(3), d(1)];
}
async function pa(t) {
  try {
    const e = await fetch(t);
    if (!e.ok) return null;
    const n = await createImageBitmap(await e.blob()), a = 24, r = new OffscreenCanvas(a, a).getContext("2d");
    r.drawImage(n, 0, 0, a, a);
    const s = r.getImageData(0, 0, a, a).data, l = (v) => {
      const A = Array.from({ length: 12 }, () => ({ w: 0, r: 0, g: 0, b: 0, best: [0, 0, 0], bestScore: 0 }));
      for (let o = 0; o < s.length; o += 4) {
        const [y, k, x] = [s[o], s[o + 1], s[o + 2]], { h: q, s: D, v: E } = _a(y, k, x);
        if (v ? D < 0.5 || E < 0.6 : D < 0.2 || E < 0.15) continue;
        const z = D * E, F = A[Math.floor(q / 30) % 12];
        F.w += z, F.r += y * z, F.g += k * z, F.b += x * z;
        const O = D * E * E;
        O > F.bestScore && (F.bestScore = O, F.best = [y, k, x]);
      }
      const C = (o) => A[o].w + 0.5 * (A[(o + 11) % 12].w + A[(o + 1) % 12].w);
      return A.map((o, y) => ({ ...o, i: y, score: C(y) })).filter((o) => o.w > 0).sort((o, y) => y.score - o.score);
    }, d = l(!0), u = d.reduce((v, A) => v + A.w, 0) > a * a * 0.04 ? d : l(!1);
    if (!u.length) return null;
    const _ = u[0], g = u.find((v) => Math.min(Math.abs(v.i - _.i), 12 - Math.abs(v.i - _.i)) >= 2) ?? _, c = (v) => [v.r / v.w, v.g / v.w, v.b / v.w], p = jt(Xt(c(_), 215));
    return {
      accent: p,
      gradient: [kt(jt(Xt(_.best, 235)), 0.2), p, kt(jt(Xt(c(g), 200)), -0.45)]
    };
  } catch {
    return null;
  }
}
async function vi(t) {
  try {
    const e = fa(new Uint8Array(await t.slice(0, 10).arrayBuffer()));
    return e ? bi(await t.slice(0, e).arrayBuffer()) : {};
  } catch {
    return {};
  }
}
function xi(t) {
  const e = t.replace(/\.[^.]+$/, "").replace(/_/g, " "), n = e.match(/^\s*(\d{1,3})\s*[-.)_ ]\s*(.+)$/);
  return n ? [parseInt(n[1], 10), n[2].trim()] : [void 0, e.trim()];
}
async function yi(t, e, n) {
  const a = t.filter((l) => l.type.startsWith("audio/") || /\.(mp3|m4a|aac|wav|ogg|oga|opus|flac|webm)$/i.test(l.name)), i = [...e], r = /* @__PURE__ */ new Set(), s = await Promise.all(a.map(async (l) => ({ f: l, tags: await vi(l) })));
  for (const { f: l, tags: d } of s) {
    const [u, _] = xi(l.name), g = d.album || n, c = d.artist || "";
    let p = i.find((A) => A.id.startsWith("u") && A.title === g && A.artist === c);
    if (p || (p = { id: "u" + ha(), title: g, artist: c, accent: Vt, gradient: da(Vt), tracks: [] }, i.push(p)), !p.cover && d.picture) {
      p.cover = URL.createObjectURL(new Blob([d.picture.data], { type: d.picture.mime }));
      const A = await pa(p.cover);
      A && Object.assign(p, A);
    }
    const v = { title: d.title || _, src: URL.createObjectURL(l), n: d.track ?? u ?? 999 };
    p.tracks.push(v), p.tracks.sort((A, C) => (A.n ?? 999) - (C.n ?? 999)), r.add(p);
  }
  return { records: i, touched: [...r] };
}
function ki(t) {
  const e = Oa(t.lang), [n] = He(() => new Ia()), [a, i] = He(t.decks === 2 && t.mode === "dj"), [r, s] = He(0), [l, d] = He([]), [u, _] = He(!1), g = ue(null), c = ue([]), p = ue(null), v = ue({ a: { playing: !1, rec: null }, b: { playing: !1, rec: null } }), A = [...t.records, ...l];
  Ee(() => () => n.close(), [n]), Ee(() => {
    c.current.forEach((x) => x.inert = !a), t.host.toggleAttribute("data-dj", a), t.host.dispatchEvent(new CustomEvent("modechange", { detail: { mode: a ? "dj" : "single" }, bubbles: !0 }));
  }, [a]), Ee(() => {
    i(t.decks === 2 && t.mode === "dj");
  }, [t.mode, t.decks]);
  const C = () => {
    a ? n.reset() : s((x) => x + 1), i(!a);
  }, o = (x, q) => {
    v.current[x] = q;
    const D = v.current.a.playing || v.current.b.playing;
    !p.current && D && n.output && g.current && (p.current = fi(n.output, g.current)), p.current?.setActive(D);
  };
  Ee(() => {
    const x = { a: 0, b: 0 }, q = window.setInterval(() => {
      const D = v.current;
      if (!p.current || !(D.a.playing || D.b.playing)) return;
      const E = [];
      ["a", "b"].forEach((z, F) => {
        const O = D[z].playing ? n.channels[F].level() : 0;
        x[z] = x[z] * 0.75 + O * 0.25, D[z].rec && E.push([D[z].rec, x[z]]);
      }), p.current.setMix(E);
    }, 80);
    return () => window.clearInterval(q);
  }, [n]);
  const y = async (x) => {
    const { records: q, touched: D } = await yi(x, l, e.myMusic);
    return d(q), D[0];
  }, k = (x, q) => /* @__PURE__ */ m(
    mi,
    {
      side: x,
      engine: n,
      channel: n.channels[x === "a" ? 0 : 1],
      dj: a,
      records: A,
      logo: t.logo,
      uploads: t.uploads,
      t: e,
      onStatus: o,
      onFiles: y,
      children: q
    }
  );
  return /* @__PURE__ */ m("div", { class: `at-root${a ? " is-dj" : ""}${u ? " has-hero" : ""}`, children: [
    /* @__PURE__ */ m("div", { ref: g, class: "at-bg-viz", "aria-hidden": "true" }),
    /* @__PURE__ */ m("div", { class: "at-inner", children: [
      /* @__PURE__ */ m("div", { class: "at-hero", children: /* @__PURE__ */ m(
        "slot",
        {
          ref: (x) => {
            if (!x || x._watched) return;
            x._watched = !0;
            const q = () => _(x.assignedNodes().some((D) => D.nodeType === 1 || D.textContent?.trim()));
            x.addEventListener("slotchange", q), q();
          }
        }
      ) }),
      k(
        "a",
        t.decks === 2 && /* @__PURE__ */ m("button", { class: "at-dj-toggle", onClick: C, hidden: a, children: e.open })
      ),
      t.decks === 2 && /* @__PURE__ */ m(Ge, { children: [
        /* @__PURE__ */ m("div", { ref: (x) => void (x && (c.current[0] = x)), class: "at-dj-wrap", children: /* @__PURE__ */ m(Ha, { engine: n, active: a, t: e, onClose: C }, r) }),
        /* @__PURE__ */ m("div", { ref: (x) => void (x && (c.current[1] = x)), class: "at-dj-wrap", children: k("b") })
      ] })
    ] })
  ] });
}
const wi = ':host{--at-accent: #c2ff3a;--at-text: #f4f2ec;--at-muted: #8c8a82;--at-font: inherit;--at-font-display: "Anton", Impact, "Arial Narrow", sans-serif;--at-font-mono: "Space Mono", ui-monospace, SFMono-Regular, Menlo, monospace;display:block;position:relative;color:var(--at-text);font-family:var(--at-font)}:host([hidden]){display:none}*{box-sizing:border-box}button,input{font:inherit}.at-root{position:relative;container-type:inline-size;overflow:hidden}.at-inner{display:flex;flex-direction:column;justify-content:center;min-height:var(--at-min-height, 780px);padding:var(--at-padding, 48px 5%)}.at-hero{position:relative;z-index:1}.at-root:not(.has-hero) .at-hero{display:none}@keyframes at-float{0%,to{transform:translateY(0) scale(1)}50%{transform:translateY(-40px) scale(1.06)}}.at-bg-viz,.at-deck-ring{position:absolute;pointer-events:none;opacity:0;transition:opacity 1.1s ease;mix-blend-mode:screen}.at-bg-viz{inset:auto 0 0;height:78%;-webkit-mask-image:linear-gradient(to top,#000 20%,rgba(0,0,0,.35) 60%,transparent);mask-image:linear-gradient(to top,#000 20%,rgba(0,0,0,.35) 60%,transparent)}.at-bg-viz.is-on{opacity:.42}.at-deck-ring{inset:-22%;filter:blur(.5px)}.at-deck-ring.is-on{opacity:.85}.at-bg-viz canvas,.at-deck-ring canvas{display:block}.at-root{--dj-ease: cubic-bezier(.65, 0, .2, 1);--w-single: clamp(260px, min(34cqw, 100vh - 370px), 540px);--dj-w: clamp(220px, min((90cqw - 380px) / 2, 100vh - 470px), 480px)}.at-deck{--deck-accent: var(--at-accent);position:absolute;top:50%;right:5cqw;z-index:1;width:var(--w-single);margin-top:30px;transform:translateY(-50%);transition:right .9s var(--dj-ease),width .9s var(--dj-ease),opacity .7s ease,transform .9s var(--dj-ease)}.at-deck-stage{position:relative;aspect-ratio:1}.at-deck-disc{position:absolute;inset:0;border-radius:50%;cursor:grab;touch-action:pan-y;animation:at-deck-in .7s cubic-bezier(.16,1,.3,1)}.at-deck-disc:active{cursor:grabbing}@keyframes at-deck-in{0%{opacity:0;transform:translate(-35%) rotate(-60deg) scale(.9)}}.at-vinyl{position:absolute;inset:0;border-radius:50%;background:conic-gradient(from 30deg,transparent 0 8%,rgba(255,255,255,.09) 12%,transparent 18% 58%,rgba(255,255,255,.06) 62%,transparent 68%),repeating-radial-gradient(circle,#111 0 2px,#1b1b1d 3px 4px),#111;box-shadow:0 0 0 1px #ffffff0f,0 40px 120px #000000b3,0 0 90px color-mix(in srgb,var(--deck-accent) 16%,transparent);transition:box-shadow .6s}.at-vinyl:after{content:"";position:absolute;top:50%;left:50%;width:2.4%;height:2.4%;margin:-1.2% 0 0 -1.2%;border-radius:50%;background:#d8d6ce;box-shadow:0 0 0 3px #0a0a0b}.at-deck-label{position:absolute;inset:31%;width:38%;height:38%;border-radius:50%;object-fit:cover;box-shadow:0 0 0 4px #0a0a0b,0 0 0 6px color-mix(in srgb,var(--deck-accent) 60%,transparent);pointer-events:none;user-select:none}.at-vinyl-logo{position:absolute;inset:17%;width:66%;height:66%;filter:drop-shadow(0 18px 40px rgba(0,0,0,.75));animation:at-float 9s ease-in-out infinite;pointer-events:none}.at-vinyl-arm{position:absolute;top:2%;left:98%;width:30px;height:62%;margin-left:-15px;background:linear-gradient(#d8d6ce,#7c7a72) center / 6px 100% no-repeat;transform-origin:50% 0;transform:rotate(-8deg);filter:drop-shadow(0 10px 18px rgba(0,0,0,.6));cursor:grab;touch-action:none}.at-vinyl-arm:active{cursor:grabbing}.at-vinyl-arm:before{content:"";position:absolute;top:-14px;left:1px;width:28px;height:28px;border-radius:50%;background:#2a2a2c;border:2px solid #7c7a72;box-sizing:border-box}.at-vinyl-arm:after{content:"";position:absolute;bottom:-8px;left:7px;width:16px;height:22px;border-radius:3px;background:var(--deck-accent);transition:background .6s}.at-deck-ui{display:flex;flex-direction:column;gap:16px;margin-top:28px}.at-deck-sleeves{display:flex;justify-content:center;gap:14px}.at-deck-sleeve{width:64px;height:64px;padding:0;border:2px solid transparent;border-radius:4px;overflow:hidden;background:none;cursor:pointer;opacity:.55;transition:opacity .3s,transform .3s,border-color .3s,box-shadow .3s}.at-deck-sleeve img{display:block;width:100%;height:100%}.at-deck-sleeve:hover{opacity:.9;transform:translateY(-3px)}.at-deck-sleeve[aria-pressed=true]{opacity:1;border-color:var(--deck-accent);transform:translateY(-5px);box-shadow:0 12px 30px color-mix(in srgb,var(--deck-accent) 35%,transparent)}.at-deck-now{display:flex;align-items:center;justify-content:space-between;gap:16px;padding-top:14px;border-top:1px solid rgba(255,255,255,.08)}.at-deck-meta{display:flex;flex-direction:column;gap:4px;min-width:0}.at-deck-band{font-family:var(--at-font-mono);font-size:11px;letter-spacing:.2em;text-transform:uppercase;color:var(--deck-accent)}.at-deck-track{font-size:15px;color:var(--at-text);white-space:nowrap;overflow:hidden;text-overflow:ellipsis}.at-deck-controls{display:flex;flex:none;gap:8px}.at-deck-btn{width:40px;height:40px;border:1px solid rgba(255,255,255,.22);border-radius:4px;background:transparent;color:var(--at-text);font-size:13px;cursor:pointer;transition:border-color .25s,color .25s,transform .25s}.at-deck-btn:hover{border-color:var(--deck-accent);color:var(--deck-accent)}.at-deck-btn-main,.at-deck-btn-main:hover{border-color:transparent;background:var(--deck-accent);color:#0a0a0b}.at-deck-btn-main:hover{transform:translateY(-2px)}.at-deck-dj{display:flex;flex-direction:column;gap:12px}.at-fader{display:flex;flex-direction:column;gap:6px;font-family:var(--at-font-mono);font-size:10px;letter-spacing:.16em;text-transform:uppercase;color:var(--at-muted);text-align:center}.at-fader input{width:100%;height:22px;margin:0;background:transparent;cursor:pointer;-webkit-appearance:none;appearance:none}.at-fader input::-webkit-slider-runnable-track{height:3px;border-radius:2px;background:linear-gradient(90deg,transparent calc(50% - 1px),rgba(255,255,255,.5) calc(50% - 1px) calc(50% + 1px),transparent calc(50% + 1px)),#ffffff24}.at-fader input::-moz-range-track{height:3px;border-radius:2px;background:#ffffff24}.at-fader input::-webkit-slider-thumb{width:14px;height:22px;margin-top:-9.5px;border:0;border-radius:3px;background:linear-gradient(var(--at-text) 0 44%,var(--deck-accent) 44% 56%,#bdbbb2 56%);box-shadow:0 4px 10px #0009;-webkit-appearance:none;appearance:none}.at-fader input::-moz-range-thumb{width:14px;height:22px;border:0;border-radius:3px;background:linear-gradient(var(--at-text) 0 44%,var(--deck-accent) 44% 56%,#bdbbb2 56%);box-shadow:0 4px 10px #0009}.at-fader input:focus-visible{outline:2px solid var(--deck-accent);outline-offset:4px}.at-pads{display:grid;grid-auto-flow:column;grid-auto-columns:1fr;gap:8px}.at-pad[aria-pressed=true]{border-color:var(--deck-accent);color:var(--deck-accent);box-shadow:inset 0 0 12px color-mix(in srgb,var(--deck-accent) 30%,transparent)}.at-pad{padding:10px 0;border:1px solid color-mix(in srgb,var(--deck-accent) 45%,transparent);border-radius:4px;background:color-mix(in srgb,var(--deck-accent) 7%,transparent);color:var(--at-text);font-family:var(--at-font-mono);font-size:11px;letter-spacing:.18em;text-transform:uppercase;cursor:pointer;user-select:none;touch-action:manipulation;transition:background .15s,box-shadow .15s,transform .1s}.at-pad:hover{background:color-mix(in srgb,var(--deck-accent) 16%,transparent)}.at-pad:active{background:var(--deck-accent);color:#0a0a0b;box-shadow:0 0 24px color-mix(in srgb,var(--deck-accent) 55%,transparent);transform:scale(.97)}.at-dj-wrap{display:contents}.at-dj-toggle{width:100%;padding:11px 0;border:1px solid rgba(255,255,255,.18);border-radius:4px;background:transparent;color:#bdbbb2;font-family:var(--at-font-mono);font-size:11px;letter-spacing:.22em;text-transform:uppercase;cursor:pointer;transition:color .25s,border-color .25s,box-shadow .25s}.at-dj-toggle:hover{color:var(--deck-accent, var(--at-accent));border-color:var(--deck-accent, var(--at-accent));box-shadow:0 0 20px color-mix(in srgb,var(--deck-accent, var(--at-accent)) 25%,transparent)}.at-hero{transition:opacity .6s ease,transform .9s var(--dj-ease)}.at-root.is-dj .at-hero{opacity:0;transform:translate(-12cqw);pointer-events:none}.at-root.is-dj .at-deck-a{right:calc(100% - 5cqw - var(--dj-w));width:var(--dj-w)}.at-deck-b{width:var(--dj-w);opacity:0;transform:translate(50cqw,-50%) rotate(25deg)}.at-root.is-dj .at-deck-b{opacity:1;transform:translateY(-50%);transition-delay:.12s}.at-root.is-dj .at-vinyl-logo{animation:at-logo-out .8s cubic-bezier(.6,0,.4,1) forwards}@keyframes at-logo-out{to{opacity:0;transform:translate(-45vw,-25vh) rotate(-280deg) scale(.3)}}.at-mixer{--deck-accent: var(--at-accent);position:absolute;top:50%;left:50%;z-index:2;width:320px;margin-top:30px;display:flex;flex-direction:column;gap:16px;padding:18px;border:1px solid rgba(255,255,255,.09);border-radius:10px;background:linear-gradient(#18181bd1,#0c0c0ee6);box-shadow:0 30px 80px #0009,inset 0 1px #ffffff0d;-webkit-backdrop-filter:blur(12px);backdrop-filter:blur(12px);opacity:0;transform:translate(-50%,-50%) translateY(80px) scale(.9);transition:opacity .6s ease,transform .9s var(--dj-ease)}.at-root.is-dj .at-mixer{opacity:1;transform:translate(-50%,-50%);transition-delay:.25s}.at-mixer-strips{display:grid;grid-template-columns:1fr 1fr}.at-mixer-strip{display:grid;grid-template-columns:1fr 1fr;justify-items:center;gap:12px 6px;padding:0 10px}.at-mixer-strip+.at-mixer-strip{border-left:1px solid rgba(255,255,255,.08)}.at-mixer-ch{grid-column:1 / -1;font-family:var(--at-font-display);font-size:22px;line-height:1;color:var(--at-accent)}.at-knob-wrap{display:flex;flex-direction:column;align-items:center;gap:6px;font-family:var(--at-font-mono);font-size:9px;letter-spacing:.16em;text-transform:uppercase;color:var(--at-muted)}.at-knob{position:relative;width:40px;height:40px;border-radius:50%;background:radial-gradient(circle at 50% 32%,#3a3a3e,#151517 70%);box-shadow:inset 0 1px #ffffff24,0 4px 10px #0009,0 0 0 3px #0c0c0e,0 0 0 4px #ffffff14;cursor:ns-resize;touch-action:none}.at-knob:after{content:"";position:absolute;top:4px;left:50%;width:3px;height:12px;margin-left:-1.5px;border-radius:2px;background:var(--at-accent);box-shadow:0 0 6px var(--at-accent);transform-origin:50% 16px;transform:rotate(var(--knob, 0deg))}.at-knob:focus-visible{outline:2px solid var(--at-accent);outline-offset:5px}.at-mixer-faderbox{grid-column:1 / -1;display:flex;gap:14px;height:120px}.at-mixer-vu{display:flex;align-items:flex-end;width:8px;border-radius:4px;background:#ffffff0f;overflow:hidden}.at-mixer-vu-fill{width:100%;height:100%;background:linear-gradient(to top,#2bd9a0,#c2ff3a 55%,#ffc23d 78%,#ff4d3d);transform:scaleY(0);transform-origin:bottom}.at-mixer-vfader{position:relative;width:22px}.at-mixer-vfader input{position:absolute;top:50%;left:50%;width:120px;transform:translate(-50%,-50%) rotate(-90deg)}.at-mixer-xfader input::-webkit-slider-thumb{width:22px}.at-mixer-xfader input::-moz-range-thumb{width:22px}@container (max-width: 1000px){.at-deck{position:relative;top:auto;right:auto;width:min(84cqw,460px);margin:64px auto 20px;transform:none}.at-deck-b,.at-mixer,.at-root.is-dj .at-hero{display:none}.at-root.is-dj .at-deck-a,.at-root.is-dj .at-deck-b{display:block;right:auto;width:min(92cqw,460px);margin:24px auto;transform:none}.at-root.is-dj .at-mixer{position:relative;top:auto;left:auto;display:flex;width:min(92cqw,460px);margin:24px auto;transform:none}}@container (min-width: 1001px){.at-root:not(.has-hero):not(.is-dj) .at-deck-a{right:calc(50% - var(--w-single) / 2)}}.at-deck-label{overflow:hidden;display:grid;place-items:center;background:var(--deck-accent)}.at-deck-label img{width:100%;height:100%;object-fit:cover}.at-deck-label span{padding:12%;font-family:var(--at-font-display);font-size:clamp(11px,1.6cqw,20px);line-height:1.05;text-align:center;text-transform:uppercase;color:#0a0a0b;overflow-wrap:anywhere}.at-deck-sleeves{overflow-x:auto;padding:6px 4px 8px;scrollbar-width:thin}.at-deck-sleeve{flex:none}.at-deck-sleeve span{display:grid;place-items:center;width:100%;height:100%;padding:4px;background:var(--sleeve-accent, var(--at-accent));color:#0a0a0b;font-family:var(--at-font-mono);font-size:9px;line-height:1.1;overflow:hidden}.at-deck-sleeve[aria-pressed=true]{border-color:var(--sleeve-accent, var(--deck-accent))}.at-deck-upload{border:2px dashed rgba(255,255,255,.28);color:var(--at-muted);font-size:28px;opacity:.8}.at-deck-upload:hover{border-color:var(--at-accent);color:var(--at-accent)}.at-deck-drop{position:absolute;inset:0;z-index:3;display:grid;place-items:center;border:2px dashed var(--at-accent);border-radius:50%;background:#0a0a0bb8;font-family:var(--at-font-mono);font-size:12px;letter-spacing:.2em;text-transform:uppercase;color:var(--at-accent);pointer-events:none}.at-deck-band a{color:inherit;text-decoration:none;border-bottom:1px solid color-mix(in srgb,var(--deck-accent) 45%,transparent)}.at-deck-band a:hover{border-color:var(--deck-accent)}.at-mixer-ch{color:var(--at-accent)}.at-powered{display:flex;align-items:center;justify-content:center;gap:6px;font-family:var(--at-font-mono);font-size:10px;letter-spacing:.16em;text-transform:uppercase;color:var(--at-muted);text-decoration:none;transition:color .25s}.at-powered img{width:18px;height:18px}.at-powered:hover{color:var(--at-text)}.at-deck-btn,.at-pad,.at-dj-toggle{font-family:var(--at-font-mono)}.at-deck-btn{font-family:inherit}@media (prefers-reduced-motion: reduce){.at-vinyl-logo,.at-deck-disc{animation:none}}';
class Ai extends HTMLElement {
  static observedAttributes = ["records", "decks", "mode", "uploads", "logo", "lang"];
  mount;
  list = [];
  loadId = 0;
  get records() {
    return this.list;
  }
  set records(e) {
    this.loadId++, this.list = qn(e, document.baseURI), this.update(), this.colorize();
  }
  connectedCallback() {
    if (!this.shadowRoot) {
      const e = this.attachShadow({ mode: "open" }), n = document.createElement("style");
      n.textContent = wi, this.mount = document.createElement("div"), e.append(n, this.mount);
    }
    this.update();
  }
  disconnectedCallback() {
    this.mount && un(null, this.mount);
  }
  attributeChangedCallback(e, n, a) {
    e === "records" && a && a !== n ? this.fetchRecords(a) : this.update();
  }
  async fetchRecords(e) {
    const n = ++this.loadId;
    try {
      const a = new URL(e, document.baseURI).href, i = await (await fetch(a)).json();
      if (n !== this.loadId) return;
      this.list = qn(i, a), this.update(), this.colorize();
    } catch (a) {
      console.error("[audiola-turntable] records konnten nicht geladen werden:", a);
    }
  }
  /** Fehlende Plattenfarben aus den Covern berechnen und neu zeichnen. */
  async colorize() {
    const e = this.list;
    await hi(e, pa) && e === this.list && (this.list = [...e], this.update());
  }
  update() {
    if (!this.mount || !this.isConnected) return;
    const e = this.getAttribute("logo");
    un(
      /* @__PURE__ */ m(
        ki,
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
customElements.get("audiola-turntable") || customElements.define("audiola-turntable", Ai);
