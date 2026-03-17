import { afterEach, describe, expect, it, vi, beforeEach } from "vitest";
import axios from "axios";
import { logout } from "./authService";

afterEach(() => {
  document.body.innerHTML = "";
  vi.unstubAllEnvs();
});

describe("login", () => {
  beforeEach(() => {
    vi.resetModules();
    document.body.innerHTML = '';
  });

  it("throws an error when VITE_AUTH_API_URL is missing", async () => {
    vi.stubEnv("VITE_AUTH_API_URL", "");

    // Import it fresh so it re-reads the empty env
    const { login } = await import("./authService");

    expect(() => login()).toThrow(
      "Missing VITE_AUTH_API_URL in environment variables."
    );
  });

  it("creates and submits a hidden POST form to the auth endpoint", async () => {
    const mockUrl = "http://localhost:8080";
    vi.stubEnv("VITE_AUTH_API_URL", mockUrl);

    const submitSpy = vi
      .spyOn(HTMLFormElement.prototype, "submit")
      .mockImplementation(() => {});

    const { login } = await import("./authService");

    login();

    // Assert against the actual DOM instead of mocking createElement
    const form = document.body.querySelector("form");
    
    expect(form).not.toBeNull();
    expect(form?.method).toBe("post"); 
    expect(form?.action).toBe(`${mockUrl}/api/auth/tokens`);
    expect(form?.style.display).toBe("none");
    expect(submitSpy).toHaveBeenCalledTimes(1);
  });
});

vi.mock("axios");

describe("logout", () => {

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("calls logout API and redirects on success", async () => {
    vi.mocked(axios.post).mockResolvedValue({});

    Object.defineProperty(globalThis, "location", {
      value: { href: "" },
      writable: true
    });

    await logout();

    expect(axios.post).toHaveBeenCalledWith(
      expect.stringContaining("/api/auth/logout"),
      {},
      { withCredentials: true }
    );

    expect(globalThis.location.href).toBe("/");
  });

  it("logs error when request fails", async () => {
    const consoleSpy = vi.spyOn(console, "error").mockImplementation(() => {});
    vi.mocked(axios.post).mockRejectedValue(new Error("Network error"));

    await logout();

    expect(consoleSpy).toHaveBeenCalled();
  });

});