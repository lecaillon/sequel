import store from '@/store';
import { ProblemDetails } from '@/models/problemDetails';
import { AppSnackbar } from '@/models/appSnackbar';

class Http {
    public async get<T>(url: string): Promise<T> {
        return await this.internalFetch(url, "GET");
    }

    public async post<T>(url: string, body: any): Promise<T> {
        return await this.internalFetch(url, "POST", body);
    }

    public async delete<T>(url: string): Promise<T> {
        return await this.internalFetch(url, "DELETE");
    }

    private async internalFetch<T>(url: string, method: string, payload?: any): Promise<T> {
        const body = payload ? JSON.stringify(payload) : undefined;
        let response: Response;

        try {
            response = await fetch(url, {
                method, body, mode: "cors", headers: { 'Content-type': 'application/json' }
            });
        } catch (error) {
            this.SendNotification(error);
            throw new Error(error);
        }

        if (response.ok) {
            if (response.headers.get('Content-Length') === "0") {
                return undefined!;
            }
            return await response.json();
        }

        if (response.headers.get('Content-Length') === "0") {
            this.SendNotification(`HTTP error ${response.status} : ${response.statusText}`);
            throw new Error(`HTTP error ${response.status}. ${method} ${url} : ${response.statusText}`);
        }

        const err = await response.json() as ProblemDetails;
        const stackTrace = err.detail !== undefined ? err.detail : "";
        const details = err.errors !== undefined ? Object.values(err.errors).map(x => String(x)) : [];
        this.SendNotification(`Network error (${err.status}): ${err.title}`, details);
        throw new Error(`HTTP error ${err.status}. ${method} ${url} : ${err.title} ${details.join(" ")} ${stackTrace}`);
    }

    private SendNotification(message: string, details?: string[]): void {
        store.dispatch("showAppSnackbar", {
            message: message,
            details: details,
            color: "error"
        } as AppSnackbar);
    }
}

export const http = new Http();