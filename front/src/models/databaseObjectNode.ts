export interface DatabaseObjectNode {
    id: string;
    name: string;
    type: string;
    icon: string;
    children: DatabaseObjectNode[];
}