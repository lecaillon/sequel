export interface ProblemDetails {
    title: string,
    status: string,
    detail?: string, // stacktrace
    type: string,
    traceId: string,
    errors?: any // validation errors
}