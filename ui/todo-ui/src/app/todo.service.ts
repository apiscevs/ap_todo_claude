import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import {
  GetTodosGQL,
  CreateTodoGQL,
  ToggleTodoGQL,
  DeleteTodoGQL,
  CreateTodoInput
} from './generated/graphql';
import { Todo } from './todo.model';

@Injectable({ providedIn: 'root' })
export class TodoService {
  private getTodosGQL = inject(GetTodosGQL);
  private createTodoGQL = inject(CreateTodoGQL);
  private toggleTodoGQL = inject(ToggleTodoGQL);
  private deleteTodoGQL = inject(DeleteTodoGQL);

  getAll() {
    return this.getTodosGQL
      .fetch()
      .pipe(
        map(result => {
          const todos = result.data?.todos || [];
          return todos.map(t => ({
            id: t.id,
            title: t.title,
            isCompleted: t.isCompleted
          })) as Todo[];
        })
      );
  }

  create(title: string) {
    const input: CreateTodoInput = { title };
    return this.createTodoGQL
      .mutate({
        variables: { input },
        refetchQueries: ['GetTodos'],
        awaitRefetchQueries: true
      })
      .pipe(
        map(result => {
          const todo = result.data?.createTodo;
          if (!todo) throw new Error('Failed to create todo');
          return {
            id: todo.id,
            title: todo.title,
            isCompleted: todo.isCompleted
          } as Todo;
        })
      );
  }

  toggle(id: number) {
    return this.toggleTodoGQL
      .mutate({
        variables: { id },
        optimisticResponse: {
          __typename: 'Mutation',
          toggleTodo: {
            __typename: 'TodoItem',
            id,
            title: '',
            isCompleted: true
          }
        },
        refetchQueries: ['GetTodos']
      })
      .pipe(
        map(result => {
          const todo = result.data?.toggleTodo;
          if (!todo) throw new Error('Failed to toggle todo');
          return {
            id: todo.id,
            title: todo.title,
            isCompleted: todo.isCompleted
          } as Todo;
        })
      );
  }

  delete(id: number) {
    return this.deleteTodoGQL
      .mutate({
        variables: { id },
        update(cache) {
          cache.evict({ id: `TodoItem:${id}` });
          cache.gc();
        }
      })
      .pipe(
        map(result => {
          if (result.data?.deleteTodo !== true) {
            throw new Error('Failed to delete todo');
          }
          return result.data.deleteTodo;
        })
      );
  }
}
