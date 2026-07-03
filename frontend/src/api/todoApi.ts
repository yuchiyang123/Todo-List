import { http } from './http'

export type Priority = 0 | 1 | 2

export interface Todo {
  id: number
  title: string
  description: string | null
  isCompleted: boolean
  priority: Priority
  dueDate: string | null
  createdAt: string
  updatedAt: string
}

export interface TodoInput {
  title: string
  description?: string | null
  priority: Priority
  dueDate?: string | null
}

export async function fetchTodos(filter: { completed?: boolean; keyword?: string } = {}): Promise<Todo[]> {
  const { data } = await http.get<Todo[]>('/todos', { params: filter })
  return data
}

export async function createTodo(input: TodoInput): Promise<Todo> {
  const { data } = await http.post<Todo>('/todos', input)
  return data
}

export async function updateTodo(id: number, input: TodoInput & { isCompleted: boolean }): Promise<Todo> {
  const { data } = await http.put<Todo>(`/todos/${id}`, input)
  return data
}

export async function toggleTodoComplete(id: number): Promise<Todo> {
  const { data } = await http.patch<Todo>(`/todos/${id}/complete`)
  return data
}

export async function deleteTodo(id: number): Promise<void> {
  await http.delete(`/todos/${id}`)
}
