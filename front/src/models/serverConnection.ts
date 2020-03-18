export interface ServerConnection {
    id?: number,
    name: string;
    type: string;
    connectionString: string;
    environment?: string;
}