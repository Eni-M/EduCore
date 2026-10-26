// A school day's traffic, at a size you choose, with the thresholds a release must meet.
//
//   docker run --rm -i -v "$PWD/loadtests:/scripts" grafana/k6:1.3.0 run /scripts/school-day.js \
//     -e BASE_URL=https://host.docker.internal:7132 -e TOKEN=<session token> -e INSECURE=1
//
// DEBUG=1 prints the status and error of every failed request.
// TOKEN is the value of a signed-in office user's session cookie (it is the JWT); without it only
// the public scenario runs. See loadtests/README.md for the rate-limit settings the instance under
// test needs, or the run measures the rate limiter instead of the API.
import http from "k6/http";
import { check, sleep } from "k6";

const BASE = __ENV.BASE_URL || "https://localhost:7132";
const TOKEN = __ENV.TOKEN || "";
const SCALE = Number(__ENV.SCALE || 1);

export const options = {
  insecureSkipTLSVerify: __ENV.INSECURE === "1", // local dev certificate only
  scenarios: {
    // The landing page and health checks: anonymous, steady.
    public: {
      executor: "constant-arrival-rate",
      exec: "publicPages",
      rate: 5 * SCALE,
      timeUnit: "1s",
      duration: "1m",
      preAllocatedVUs: 10 * SCALE,
    },
    // Staff working through the morning: dashboard, bell, lists, a statistics page.
    signed_in: {
      executor: "ramping-vus",
      exec: "staffDay",
      startVUs: 0,
      stages: [
        { duration: "20s", target: 10 * SCALE },
        { duration: "40s", target: 10 * SCALE },
        { duration: "10s", target: 0 },
      ],
    },
  },
  thresholds: {
    http_req_failed: ["rate<0.01"],
    "http_req_duration{scenario:public}": ["p(95)<500"],
    "http_req_duration{scenario:signed_in}": ["p(95)<800"],
    checks: ["rate>0.99"],
  },
};

export function publicPages() {
  const health = http.get(`${BASE}/healthz`);
  check(health, { "health 200": (r) => r.status === 200 });

  const stats = http.get(`${BASE}/api/public/stats`);
  check(stats, { "public stats 200": (r) => r.status === 200 });
  if (__ENV.DEBUG && stats.status !== 200) console.warn(`public stats: ${stats.status} ${stats.error || ""}`);
}

export function staffDay() {
  if (!TOKEN) {
    sleep(1);
    return;
  }

  const auth = { headers: { Authorization: `Bearer ${TOKEN}` } };
  const get = (path, name) => {
    const res = http.get(`${BASE}${path}`, Object.assign({ tags: { name } }, auth));
    check(res, { [`${name} 200`]: (r) => r.status === 200 });
    if (__ENV.DEBUG && res.status !== 200) console.warn(`${name}: ${res.status} ${res.error || ""}`);
  };

  get("/api/auth/me", "me");
  get("/api/dashboard", "dashboard");
  get("/api/notifications/unread-count", "bell");
  get("/api/students?page=1&pageSize=20", "students");
  get("/api/guardians?page=1&pageSize=20", "guardians");
  get("/api/statistics/school-overview", "statistics overview");

  // A person reads what they asked for before asking again.
  sleep(1 + Math.random() * 2);
}
