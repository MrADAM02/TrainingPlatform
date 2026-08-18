// k6 load test against PRD §8.2's targets: "tens of simultaneous users" and
// "API responses for list/search/dashboard endpoints target < 500ms under normal load".
//
// Run: docker run --rm --add-host=host.docker.internal:host-gateway -v <this-dir>:/scripts \
//        -e API_BASE=http://host.docker.internal:5080/api/v1 \
//        -e LOGIN_EMAIL=... -e LOGIN_PASSWORD=... \
//        grafana/k6 run /scripts/load-test.js
import http from 'k6/http'
import { check, sleep } from 'k6'

const API_BASE = __ENV.API_BASE || 'http://host.docker.internal:5080/api/v1'
const LOGIN_EMAIL = __ENV.LOGIN_EMAIL
const LOGIN_PASSWORD = __ENV.LOGIN_PASSWORD

export const options = {
  scenarios: {
    tens_of_concurrent_users: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '10s', target: 30 },
        { duration: '20s', target: 30 },
        { duration: '5s', target: 0 },
      ],
    },
  },
  thresholds: {
    'http_req_duration{endpoint:courses}': ['p(95)<500'],
    'http_req_duration{endpoint:search}': ['p(95)<500'],
    'http_req_duration{endpoint:dashboard}': ['p(95)<500'],
    http_req_failed: ['rate<0.01'],
  },
}

export function setup() {
  const res = http.post(
    `${API_BASE}/auth/login`,
    JSON.stringify({ email: LOGIN_EMAIL, password: LOGIN_PASSWORD }),
    { headers: { 'Content-Type': 'application/json' } },
  )
  check(res, { 'login succeeded': r => r.status === 200 })
  return { token: res.json('accessToken') }
}

export default function (data) {
  const headers = { Authorization: `Bearer ${data.token}` }

  const coursesRes = http.get(`${API_BASE}/courses?page=1&pageSize=20`, {
    headers,
    tags: { endpoint: 'courses' },
  })
  check(coursesRes, { 'courses: status 200': r => r.status === 200 })

  const searchRes = http.get(`${API_BASE}/search/documents?keyword=a&page=1&pageSize=20`, {
    headers,
    tags: { endpoint: 'search' },
  })
  check(searchRes, { 'search: status 200': r => r.status === 200 })

  const dashboardRes = http.get(`${API_BASE}/dashboard`, {
    headers,
    tags: { endpoint: 'dashboard' },
  })
  check(dashboardRes, { 'dashboard: status 200': r => r.status === 200 })

  sleep(1)
}
