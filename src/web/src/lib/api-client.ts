// One place that knows how to call the VivaBev API.
// The API answers errors with RFC 9457 ProblemDetails; we turn those into ApiError.

export type ProblemDetails = {
  type?: string
  title?: string
  status?: number
  detail?: string
  errors?: Record<string, string[]> // validation errors, keyed by field
}

export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? `Request failed with status ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}

type RequestOptions = {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  body?: unknown
  signal?: AbortSignal
}

export async function api<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' }
  if (options.body !== undefined) headers['Content-Type'] = 'application/json'

  const response = await fetch(path, {
    method: options.method ?? 'GET',
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
    credentials: 'same-origin', // send the auth cookie
    signal: options.signal,
  })

  if (!response.ok) throw new ApiError(response.status, await readProblem(response))
  if (response.status === 204) return undefined as T
  return (await response.json()) as T
}

async function readProblem(response: Response): Promise<ProblemDetails> {
  if (response.headers.get('content-type')?.includes('json')) {
    try {
      return (await response.json()) as ProblemDetails
    } catch {
      // empty or malformed body: fall through
    }
  }
  return { status: response.status, title: response.statusText || 'Request failed' }
}
