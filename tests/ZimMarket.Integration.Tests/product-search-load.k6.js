import http from "k6/http";
import { check } from "k6";

const baseUrl = __ENV.BASE_URL || "http://localhost:5000";
const searchTerm = __ENV.SEARCH_TERM || "integration";
const pageSize = Number(__ENV.PAGE_SIZE || 20);

export const options = {
  scenarios: {
    product_search: {
      executor: "constant-vus",
      vus: 200,
      duration: "60s",
      gracefulStop: "5s",
    },
  },
  thresholds: {
    http_req_duration: ["p(95)<500"],
    http_req_failed: ["rate<0.01"],
  },
  summaryTrendStats: ["avg", "min", "med", "p(90)", "p(95)", "max"],
};

export default function () {
  const url =
    `${baseUrl}/api/v1/products` +
    `?searchTerm=${encodeURIComponent(searchTerm)}` +
    `&page=1&pageSize=${pageSize}`;

  const response = http.get(url, {
    tags: { endpoint: "products-search" },
    timeout: "10s",
  });

  check(response, {
    "search endpoint returns 200": (r) => r.status === 200,
  });
}
