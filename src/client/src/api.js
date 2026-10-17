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

  const data = await response.json().catch(() => ({}));

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

  const data = await response.json().catch(() => ({}));

  if (!response.ok) {
    throw new Error(extractErrorMessage(data, "Login failed."));
  }

  return data; // { token: "..." }
}

// for any endpoint that requires a JWT.
async function authFetch(path, options = {}) {
  const token = localStorage.getItem("token");

  // File uploads are sent as FormData. The browser must set that Content-Type
  // itself (it includes a generated boundary), so JSON is only the default
  // for everything else.
  const isFormData = options.body instanceof FormData;

  let response;
  try {
    response = await fetch(`${API_BASE}${path}`, {
      ...options,
      headers: {
        ...(isFormData ? {} : { "Content-Type": "application/json" }),
        Authorization: `Bearer ${token}`,
        ...options.headers,
      },
    });
  } catch {
    throw new Error("Could not reach the server. Please try again.");
  }

  // Some error responses (e.g. 401 from the JWT middleware) have an empty body,
  // so parsing must not throw.
  const data = await response.json().catch(() => ({}));

  if (!response.ok) {
    if (response.status === 401 && !data.message) {
      throw new Error("Your session has expired. Please log in again.");
    }
    throw new Error(extractErrorMessage(data, "Request failed."));
  }

  return data;
}

// Fetches the languages the user can choose from.
export async function getLanguages() {
  let response;
  try {
    response = await fetch(`${API_BASE}/api/languages`);
  } catch {
    throw new Error("Could not reach the server. Please try again.");
  }

  if (!response.ok) {
    throw new Error("Could not load languages.");
  }

  return response.json();
}

// Gets the language the user has already saved, if any.
export async function getSelectedLanguage() {
  return authFetch("/api/languages/selected"); // { language: "Italian" | null }
}

// Sets the user's selected learning language.
export async function selectLanguage(language) {
  return authFetch("/api/languages/select", {
    method: "PUT",
    body: JSON.stringify({ language }),
  });
}

// Fetches the categories the user can choose from (public endpoint).
export async function getCategories() {
  let response;
  try {
    response = await fetch(`${API_BASE}/api/categories`);
  } catch {
    throw new Error("Could not reach the server. Please try again.");
  }

  if (!response.ok) {
    throw new Error("Could not load categories.");
  }

  return response.json(); // ["Nature", "Culture", ...]
}

// Gets the category the user has already saved, if any.
export async function getSelectedCategory() {
  return authFetch("/api/categories/selected"); // { category: "Food" | null }
}

// Sends the user's selected category to the backend.
export async function selectCategory(category) {
  return authFetch("/api/categories/select", {
    method: "PUT",
    body: JSON.stringify({ category }),
  });
}

// Uploads a new profile picture as multipart/form-data.
// The backend expects the file under the field name "file".
export async function uploadProfilePicture(file) {
  const formData = new FormData();
  formData.append("file", file);

  return authFetch("/api/profile/picture", {
    method: "POST",
    body: formData,
  });
}

// Gets the user's profile picture as a Blob, or null if none was uploaded yet.
// An <img> tag can't send the Authorization header, so the image is fetched
// here and shown through an object URL instead.
export async function getProfilePicture() {
  const token = localStorage.getItem("token");

  let response;
  try {
    response = await fetch(`${API_BASE}/api/profile/picture`, {
      headers: { Authorization: `Bearer ${token}` },
    });
  } catch {
    throw new Error("Could not reach the server. Please try again.");
  }

  if (response.status === 404) {
    return null;
  }

  if (response.status === 401) {
    throw new Error("Your session has expired. Please log in again.");
  }

  if (!response.ok) {
    throw new Error("Could not load profile picture.");
  }

  return response.blob();
}