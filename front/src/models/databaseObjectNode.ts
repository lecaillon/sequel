export interface DatabaseObjectNode {
    name: string;
    type: string;
    path: string;
    icon: string;
    children: DatabaseObjectNode[];
}