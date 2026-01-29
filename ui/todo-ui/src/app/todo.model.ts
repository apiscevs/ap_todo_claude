export type Priority = 'LOW' | 'MEDIUM' | 'HIGH';

export interface Todo {
  id: number;
  title: string;
  description?: string | null;
  isCompleted: boolean;
  priority: Priority;
}
