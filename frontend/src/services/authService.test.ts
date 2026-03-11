import { afterEach, describe, expect, it, vi, beforeEach } from "vitest";

afterEach(() => {
  document.body.innerHTML = "";
  vi.unstubAllEnvs();
});

describe("loginWithEmptyBody", () => {
  beforeEach(() => {
    vi.resetModules();
    document.body.innerHTML = '';
  });

  it("throws an error when VITE_AUTH_API_URL is missing", async () => {
    vi.stubEnv("VITE_AUTH_API_URL", "");

    // 2. Import it fresh so it re-reads the empty env
    const { loginWithEmptyBody } = await import("./authService");

    expect(() => loginWithEmptyBody()).toThrow(
      "Missing VITE_AUTH_API_URL in environment variables."
    );
  });

  it("creates and submits a hidden POST form to the auth endpoint", async () => {
    const mockUrl = "http://localhost:8080";
    vi.stubEnv("VITE_AUTH_API_URL", mockUrl);

    const submitSpy = vi
      .spyOn(HTMLFormElement.prototype, "submit")
      .mockImplementation(() => {});

    const { loginWithEmptyBody } = await import("./authService");

    loginWithEmptyBody();

    // Assert against the ACTUAL DOM instead of mocking createElement
    const form = document.body.querySelector("form");
    
    expect(form).not.toBeNull();
    expect(form?.method).toBe("post"); 
    expect(form?.action).toBe(`${mockUrl}/api/auth/tokens`);
    expect(form?.style.display).toBe("none");
    expect(submitSpy).toHaveBeenCalledTimes(1);
  });
});
