export interface TreeViewNode {
    id: string;
    name: string;
    type: string;
    icon: string;
    color: string;
    children: TreeViewNode[];
    details: Map<string, Object>;
}