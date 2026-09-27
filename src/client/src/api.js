// Central place for all calls to the backend API.
// Keeping this separate means components don't need to know
// the URL or fetch details — they just call register()/login().

const API_BASE = "http://localhost:5158";

function extractErrorMessage(data, fallback) {
  // ASP.NET's [ApiController] validation errors come back as:
  // { errors: { FieldName: ["message"] } } instead of a flat message.
  if (data.errors) {
    const firstField = Object.keys(data.errors)[0];
    return data.errors[firstField][0];
  }
  return data.message || fallback;
}

export async function register(email, username, password) {
  const response = await fetch(`${API_BASE}/api/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, username, password }),
  });

  const data = await response.json();

  if (!response.ok) {
    throw new Error(extractErrorMessage(data, "Registration failed."));
  }

  return data;
}

export async function login(email, password) {
  const response = await fetch(`${API_BASE}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });

  const data = await response.json();

  if (!response.ok) {
    throw new Error(extractErrorMessage(data, "Login failed."));
  }

  return data; // { token: "..." }
}

// for any endpoint that requires a JWT.
async function authFetch(path, options = {}) {
  const token = localStorage.getItem("token");

  const response = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
      ...options.headers,
    },
  });

  const data = await response.json();

  if (!response.ok) {
    throw new Error(extractErrorMessage(data, "Request failed."));
  }

  return data;
}

// Fetches the languages the user can choose from.
export async function getLanguages() {
  const response = await fetch(`${API_BASE}/api/languages`);
  return response.json();
}

// Sets the user's selected learning language.
export async function selectLanguage(language) {
  return authFetch("/api/languages/select", {
    method: "PUT",
    body: JSON.stringify({ language }),
  });
}