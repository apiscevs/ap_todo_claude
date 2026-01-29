import { gql } from 'apollo-angular';
import { Injectable } from '@angular/core';
import * as Apollo from 'apollo-angular';
export type Maybe<T> = T | null;
export type InputMaybe<T> = Maybe<T>;
export type Exact<T extends { [key: string]: unknown }> = { [K in keyof T]: T[K] };
export type MakeOptional<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]?: Maybe<T[SubKey]> };
export type MakeMaybe<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]: Maybe<T[SubKey]> };
export type MakeEmpty<T extends { [key: string]: unknown }, K extends keyof T> = { [_ in K]?: never };
export type Incremental<T> = T | { [P in keyof T]?: P extends ' $fragmentName' | '__typename' ? T[P] : never };
/** All built-in and custom scalars, mapped to their actual values */
export type Scalars = {
  ID: { input: string; output: string; }
  String: { input: string; output: string; }
  Boolean: { input: boolean; output: boolean; }
  Int: { input: number; output: number; }
  Float: { input: number; output: number; }
  /** The `DateTime` scalar represents an ISO-8601 compliant date time type. */
  DateTime: { input: string; output: string; }
};

export type BooleanOperationFilterInput = {
  eq?: InputMaybe<Scalars['Boolean']['input']>;
  neq?: InputMaybe<Scalars['Boolean']['input']>;
};

export type CreateTodoInput = {
  description?: InputMaybe<Scalars['String']['input']>;
  endAtUtc?: InputMaybe<Scalars['DateTime']['input']>;
  priority?: TodoPriority;
  startAtUtc?: InputMaybe<Scalars['DateTime']['input']>;
  title: Scalars['String']['input'];
};

export type DateTimeOperationFilterInput = {
  eq?: InputMaybe<Scalars['DateTime']['input']>;
  gt?: InputMaybe<Scalars['DateTime']['input']>;
  gte?: InputMaybe<Scalars['DateTime']['input']>;
  in?: InputMaybe<Array<InputMaybe<Scalars['DateTime']['input']>>>;
  lt?: InputMaybe<Scalars['DateTime']['input']>;
  lte?: InputMaybe<Scalars['DateTime']['input']>;
  neq?: InputMaybe<Scalars['DateTime']['input']>;
  ngt?: InputMaybe<Scalars['DateTime']['input']>;
  ngte?: InputMaybe<Scalars['DateTime']['input']>;
  nin?: InputMaybe<Array<InputMaybe<Scalars['DateTime']['input']>>>;
  nlt?: InputMaybe<Scalars['DateTime']['input']>;
  nlte?: InputMaybe<Scalars['DateTime']['input']>;
};

export type IntOperationFilterInput = {
  eq?: InputMaybe<Scalars['Int']['input']>;
  gt?: InputMaybe<Scalars['Int']['input']>;
  gte?: InputMaybe<Scalars['Int']['input']>;
  in?: InputMaybe<Array<InputMaybe<Scalars['Int']['input']>>>;
  lt?: InputMaybe<Scalars['Int']['input']>;
  lte?: InputMaybe<Scalars['Int']['input']>;
  neq?: InputMaybe<Scalars['Int']['input']>;
  ngt?: InputMaybe<Scalars['Int']['input']>;
  ngte?: InputMaybe<Scalars['Int']['input']>;
  nin?: InputMaybe<Array<InputMaybe<Scalars['Int']['input']>>>;
  nlt?: InputMaybe<Scalars['Int']['input']>;
  nlte?: InputMaybe<Scalars['Int']['input']>;
};

export type Mutation = {
  __typename?: 'Mutation';
  createTodo: TodoItem;
  deleteTodo: Scalars['Boolean']['output'];
  toggleTodo: TodoItem;
  updateTodo: TodoItem;
};


export type MutationCreateTodoArgs = {
  input: CreateTodoInput;
};


export type MutationDeleteTodoArgs = {
  id: Scalars['Int']['input'];
};


export type MutationToggleTodoArgs = {
  id: Scalars['Int']['input'];
};


export type MutationUpdateTodoArgs = {
  id: Scalars['Int']['input'];
  input: UpdateTodoInput;
};

export type Query = {
  __typename?: 'Query';
  todoById?: Maybe<TodoItem>;
  todos: Array<TodoItem>;
};


export type QueryTodoByIdArgs = {
  id: Scalars['Int']['input'];
};


export type QueryTodosArgs = {
  order?: InputMaybe<Array<TodoItemSortInput>>;
  where?: InputMaybe<TodoItemFilterInput>;
};

export enum SortEnumType {
  Asc = 'ASC',
  Desc = 'DESC'
}

export type StringOperationFilterInput = {
  and?: InputMaybe<Array<StringOperationFilterInput>>;
  contains?: InputMaybe<Scalars['String']['input']>;
  endsWith?: InputMaybe<Scalars['String']['input']>;
  eq?: InputMaybe<Scalars['String']['input']>;
  in?: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>;
  ncontains?: InputMaybe<Scalars['String']['input']>;
  nendsWith?: InputMaybe<Scalars['String']['input']>;
  neq?: InputMaybe<Scalars['String']['input']>;
  nin?: InputMaybe<Array<InputMaybe<Scalars['String']['input']>>>;
  nstartsWith?: InputMaybe<Scalars['String']['input']>;
  or?: InputMaybe<Array<StringOperationFilterInput>>;
  startsWith?: InputMaybe<Scalars['String']['input']>;
};

export type TodoItem = {
  __typename?: 'TodoItem';
  description?: Maybe<Scalars['String']['output']>;
  endAtUtc?: Maybe<Scalars['DateTime']['output']>;
  id: Scalars['Int']['output'];
  isCompleted: Scalars['Boolean']['output'];
  priority: TodoPriority;
  startAtUtc?: Maybe<Scalars['DateTime']['output']>;
  title: Scalars['String']['output'];
};

export type TodoItemFilterInput = {
  and?: InputMaybe<Array<TodoItemFilterInput>>;
  description?: InputMaybe<StringOperationFilterInput>;
  endAtUtc?: InputMaybe<DateTimeOperationFilterInput>;
  id?: InputMaybe<IntOperationFilterInput>;
  isCompleted?: InputMaybe<BooleanOperationFilterInput>;
  or?: InputMaybe<Array<TodoItemFilterInput>>;
  priority?: InputMaybe<TodoPriorityOperationFilterInput>;
  startAtUtc?: InputMaybe<DateTimeOperationFilterInput>;
  title?: InputMaybe<StringOperationFilterInput>;
};

export type TodoItemSortInput = {
  description?: InputMaybe<SortEnumType>;
  endAtUtc?: InputMaybe<SortEnumType>;
  id?: InputMaybe<SortEnumType>;
  isCompleted?: InputMaybe<SortEnumType>;
  priority?: InputMaybe<SortEnumType>;
  startAtUtc?: InputMaybe<SortEnumType>;
  title?: InputMaybe<SortEnumType>;
};

export enum TodoPriority {
  High = 'HIGH',
  Low = 'LOW',
  Medium = 'MEDIUM'
}

export type TodoPriorityOperationFilterInput = {
  eq?: InputMaybe<TodoPriority>;
  in?: InputMaybe<Array<TodoPriority>>;
  neq?: InputMaybe<TodoPriority>;
  nin?: InputMaybe<Array<TodoPriority>>;
};

export type UpdateTodoInput = {
  description?: InputMaybe<Scalars['String']['input']>;
  endAtUtc?: InputMaybe<Scalars['DateTime']['input']>;
  isCompleted: Scalars['Boolean']['input'];
  priority: TodoPriority;
  startAtUtc?: InputMaybe<Scalars['DateTime']['input']>;
  title: Scalars['String']['input'];
};

export type GetTodosQueryVariables = Exact<{ [key: string]: never; }>;


export type GetTodosQuery = { __typename?: 'Query', todos: Array<{ __typename?: 'TodoItem', id: number, title: string, isCompleted: boolean, priority: TodoPriority, description?: string | null, startAtUtc?: string | null, endAtUtc?: string | null }> };

export type CreateTodoMutationVariables = Exact<{
  input: CreateTodoInput;
}>;


export type CreateTodoMutation = { __typename?: 'Mutation', createTodo: { __typename?: 'TodoItem', id: number, title: string, isCompleted: boolean, priority: TodoPriority, description?: string | null, startAtUtc?: string | null, endAtUtc?: string | null } };

export type UpdateTodoMutationVariables = Exact<{
  id: Scalars['Int']['input'];
  input: UpdateTodoInput;
}>;


export type UpdateTodoMutation = { __typename?: 'Mutation', updateTodo: { __typename?: 'TodoItem', id: number, title: string, isCompleted: boolean, priority: TodoPriority, description?: string | null, startAtUtc?: string | null, endAtUtc?: string | null } };

export type ToggleTodoMutationVariables = Exact<{
  id: Scalars['Int']['input'];
}>;


export type ToggleTodoMutation = { __typename?: 'Mutation', toggleTodo: { __typename?: 'TodoItem', id: number, title: string, isCompleted: boolean, priority: TodoPriority, description?: string | null, startAtUtc?: string | null, endAtUtc?: string | null } };

export type DeleteTodoMutationVariables = Exact<{
  id: Scalars['Int']['input'];
}>;


export type DeleteTodoMutation = { __typename?: 'Mutation', deleteTodo: boolean };

export const GetTodosDocument = gql`
    query GetTodos {
  todos {
    id
    title
    isCompleted
    priority
    description
    startAtUtc
    endAtUtc
  }
}
    `;

  @Injectable({
    providedIn: 'root'
  })
  export class GetTodosGQL extends Apollo.Query<GetTodosQuery, GetTodosQueryVariables> {
    document = GetTodosDocument;
    
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const CreateTodoDocument = gql`
    mutation CreateTodo($input: CreateTodoInput!) {
  createTodo(input: $input) {
    id
    title
    isCompleted
    priority
    description
    startAtUtc
    endAtUtc
  }
}
    `;

  @Injectable({
    providedIn: 'root'
  })
  export class CreateTodoGQL extends Apollo.Mutation<CreateTodoMutation, CreateTodoMutationVariables> {
    document = CreateTodoDocument;
    
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const UpdateTodoDocument = gql`
    mutation UpdateTodo($id: Int!, $input: UpdateTodoInput!) {
  updateTodo(id: $id, input: $input) {
    id
    title
    isCompleted
    priority
    description
    startAtUtc
    endAtUtc
  }
}
    `;

  @Injectable({
    providedIn: 'root'
  })
  export class UpdateTodoGQL extends Apollo.Mutation<UpdateTodoMutation, UpdateTodoMutationVariables> {
    document = UpdateTodoDocument;
    
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const ToggleTodoDocument = gql`
    mutation ToggleTodo($id: Int!) {
  toggleTodo(id: $id) {
    id
    title
    isCompleted
    priority
    description
    startAtUtc
    endAtUtc
  }
}
    `;

  @Injectable({
    providedIn: 'root'
  })
  export class ToggleTodoGQL extends Apollo.Mutation<ToggleTodoMutation, ToggleTodoMutationVariables> {
    document = ToggleTodoDocument;
    
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }
export const DeleteTodoDocument = gql`
    mutation DeleteTodo($id: Int!) {
  deleteTodo(id: $id)
}
    `;

  @Injectable({
    providedIn: 'root'
  })
  export class DeleteTodoGQL extends Apollo.Mutation<DeleteTodoMutation, DeleteTodoMutationVariables> {
    document = DeleteTodoDocument;
    
    constructor(apollo: Apollo.Apollo) {
      super(apollo);
    }
  }