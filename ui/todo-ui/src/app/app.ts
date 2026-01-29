import { Component, computed, effect, inject, OnInit, signal } from '@angular/core';
import { CommonModule, DOCUMENT } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TodoService } from './todo.service';
import { Todo, Priority } from './todo.model';
import { UpdateTodoInput } from './generated/graphql';

type Filter = 'all' | 'active' | 'completed';
type Tab = 'todos' | 'calendar';
type CalendarView = 'day' | 'week' | 'month';

type CalendarCell = {
  date: Date;
  isCurrentMonth: boolean;
  isToday: boolean;
  isSelected: boolean;
  items: Todo[];
};

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  private todoService = inject(TodoService);
  private document = inject(DOCUMENT);

  readonly PRIORITIES: Priority[] = ['LOW', 'MEDIUM', 'HIGH'];
  readonly MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
  readonly WEEKDAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

  todos = signal<Todo[]>([]);
  newTitle = signal('');
  newDescription = signal('');
  newPriority = signal<Priority>('MEDIUM');
  newStartAt = signal('');
  newEndAt = signal('');

  editingId = signal<number | null>(null);
  editTitle = signal('');
  editDescription = signal('');
  editPriority = signal<Priority>('MEDIUM');
  editStartAt = signal('');
  editEndAt = signal('');
  editIsCompleted = signal(false);

  activeTab = signal<Tab>('todos');
  filter = signal<Filter>('all');
  errorMessage = signal('');
  darkMode = signal(false);

  calendarView = signal<CalendarView>('month');
  calendarDate = signal<Date>(new Date());

  filteredTodos = computed(() => {
    const todos = this.todos();
    switch (this.filter()) {
      case 'active':
        return todos.filter((t) => !t.isCompleted);
      case 'completed':
        return todos.filter((t) => t.isCompleted);
      default:
        return todos;
    }
  });

  scheduledTodos = computed(() =>
    this.todos().filter((t) => t.startAtUtc && t.endAtUtc)
  );

  unscheduledCount = computed(() =>
    this.todos().filter((t) => !t.startAtUtc || !t.endAtUtc).length
  );

  monthCells = computed(() => this.buildMonthGrid(this.calendarDate()));

  weekDays = computed(() => this.buildWeekDays(this.calendarDate()));

  dayTodos = computed(() =>
    this.getTodosForDate(this.calendarDate()).sort((a, b) =>
      this.getTodoStart(a)!.getTime() - this.getTodoStart(b)!.getTime()
    )
  );

  constructor() {
    const saved = localStorage.getItem('todo-dark-mode');
    if (saved === 'true' || (!saved && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
      this.darkMode.set(true);
    }

    effect(() => {
      const dark = this.darkMode();
      this.document.documentElement.setAttribute('data-theme', dark ? 'dark' : 'light');
      localStorage.setItem('todo-dark-mode', String(dark));
    });
  }

  ngOnInit() {
    this.load();
  }

  load() {
    this.todoService.getAll().subscribe({
      next: (t) => this.todos.set(t),
      error: () => this.showError('Failed to load todos'),
    });
  }

  setTab(tab: Tab) {
    this.activeTab.set(tab);
  }

  add() {
    const title = this.newTitle().trim();
    if (!title) return;

    const description = this.newDescription().trim();
    const schedule = this.resolveSchedule(this.newStartAt(), this.newEndAt());
    if (!schedule) return;

    this.todoService
      .create(title, this.newPriority(), description, schedule.startAtUtc, schedule.endAtUtc)
      .subscribe({
        next: () => {
          this.newTitle.set('');
          this.newDescription.set('');
          this.newPriority.set('MEDIUM');
          this.newStartAt.set('');
          this.newEndAt.set('');
          this.load();
        },
        error: () => this.showError('Failed to add todo'),
      });
  }

  startEdit(todo: Todo) {
    this.editingId.set(todo.id);
    this.editTitle.set(todo.title);
    this.editDescription.set(todo.description ?? '');
    this.editPriority.set(todo.priority);
    this.editIsCompleted.set(todo.isCompleted);
    this.editStartAt.set(this.toLocalInputValue(todo.startAtUtc));
    this.editEndAt.set(this.toLocalInputValue(todo.endAtUtc));
  }

  cancelEdit() {
    this.editingId.set(null);
  }

  saveEdit() {
    const id = this.editingId();
    if (id === null) return;

    const title = this.editTitle().trim();
    if (!title) {
      this.showError('Title cannot be empty');
      return;
    }

    const schedule = this.resolveSchedule(this.editStartAt(), this.editEndAt());
    if (!schedule) return;

    const input: UpdateTodoInput = {
      title,
      description: this.editDescription().trim() || '',
      priority: this.editPriority() as any,
      isCompleted: this.editIsCompleted(),
      startAtUtc: schedule.startAtUtc,
      endAtUtc: schedule.endAtUtc
    };

    this.todoService.update(id, input).subscribe({
      next: () => {
        this.editingId.set(null);
        this.load();
      },
      error: () => this.showError('Failed to update todo'),
    });
  }

  toggle(id: number) {
    this.todoService.toggle(id).subscribe({
      next: () => this.load(),
      error: () => this.showError('Failed to update todo'),
    });
  }

  deleteTodo(id: number) {
    this.todoService.delete(id).subscribe({
      next: () => this.load(),
      error: () => this.showError('Failed to delete todo'),
    });
  }

  setFilter(f: Filter) {
    this.filter.set(f);
  }

  toggleDarkMode() {
    this.darkMode.update((v) => !v);
  }

  setCalendarView(view: CalendarView) {
    this.calendarView.set(view);
  }

  goToToday() {
    this.calendarDate.set(new Date());
  }

  selectDay(date: Date) {
    this.calendarDate.set(new Date(date));
    this.calendarView.set('day');
  }

  changeMonth(delta: number) {
    const current = this.calendarDate();
    const next = new Date(current);
    next.setMonth(next.getMonth() + delta);
    this.calendarDate.set(next);
  }

  changeWeek(delta: number) {
    const current = this.calendarDate();
    const next = new Date(current);
    next.setDate(next.getDate() + delta * 7);
    this.calendarDate.set(next);
  }

  changeDay(delta: number) {
    const current = this.calendarDate();
    const next = new Date(current);
    next.setDate(next.getDate() + delta);
    this.calendarDate.set(next);
  }

  setMonth(monthIndex: number) {
    const current = this.calendarDate();
    const next = new Date(current);
    next.setMonth(monthIndex);
    this.calendarDate.set(next);
  }

  setYear(year: number) {
    const current = this.calendarDate();
    const next = new Date(current);
    next.setFullYear(year);
    this.calendarDate.set(next);
  }

  getPriorityLabel(priority: Priority): string {
    return priority.charAt(0) + priority.slice(1).toLowerCase();
  }

  getCalendarTitle(): string {
    const date = this.calendarDate();
    const view = this.calendarView();
    if (view === 'month') {
      return `${this.MONTHS[date.getMonth()]} ${date.getFullYear()}`;
    }

    if (view === 'week') {
      const week = this.buildWeekDays(date);
      const start = week[0];
      const end = week[6];
      return `${this.MONTHS[start.getMonth()]} ${start.getDate()} — ${this.MONTHS[end.getMonth()]} ${end.getDate()}, ${end.getFullYear()}`;
    }

    return `${this.MONTHS[date.getMonth()]} ${date.getDate()}, ${date.getFullYear()}`;
  }

  getTodoTimeRange(todo: Todo): string {
    const start = this.getTodoStart(todo);
    const end = this.getTodoEnd(todo);
    if (!start || !end) return '';
    const formatter = new Intl.DateTimeFormat(undefined, {
      hour: '2-digit',
      minute: '2-digit',
    });
    return `${formatter.format(start)} – ${formatter.format(end)}`;
  }

  getTodoDateLabel(todo: Todo): string {
    const start = this.getTodoStart(todo);
    if (!start) return '';
    return new Intl.DateTimeFormat(undefined, {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
    }).format(start);
  }

  getMonthIndex(): number {
    return this.calendarDate().getMonth();
  }

  getYear(): number {
    return this.calendarDate().getFullYear();
  }

  getYearOptions(): number[] {
    const year = this.getYear();
    return Array.from({ length: 9 }, (_, i) => year - 4 + i);
  }

  getWeekDayTodos(day: Date): Todo[] {
    return this.getTodosForDate(day).sort((a, b) =>
      this.getTodoStart(a)!.getTime() - this.getTodoStart(b)!.getTime()
    );
  }

  private resolveSchedule(startValue: string, endValue: string) {
    if (!startValue && !endValue) {
      return { startAtUtc: null, endAtUtc: null };
    }

    if (!startValue || !endValue) {
      this.showError('Start and end time are both required.');
      return null;
    }

    const start = new Date(startValue);
    const end = new Date(endValue);

    if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime())) {
      this.showError('Invalid date or time.');
      return null;
    }

    if (end < start) {
      this.showError('End time must be after start time.');
      return null;
    }

    return { startAtUtc: start.toISOString(), endAtUtc: end.toISOString() };
  }

  private getTodoStart(todo: Todo): Date | null {
    return todo.startAtUtc ? new Date(todo.startAtUtc) : null;
  }

  private getTodoEnd(todo: Todo): Date | null {
    return todo.endAtUtc ? new Date(todo.endAtUtc) : null;
  }

  private toLocalInputValue(value?: string | null): string {
    if (!value) return '';
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '';
    const offset = date.getTimezoneOffset();
    const local = new Date(date.getTime() - offset * 60000);
    return local.toISOString().slice(0, 16);
  }

  private buildMonthGrid(date: Date): CalendarCell[] {
    const firstOfMonth = new Date(date.getFullYear(), date.getMonth(), 1);
    const start = this.getWeekStart(firstOfMonth);
    const cells: CalendarCell[] = [];
    for (let i = 0; i < 42; i++) {
      const day = new Date(start);
      day.setDate(start.getDate() + i);
      const items = this.getTodosForDate(day);
      cells.push({
        date: day,
        isCurrentMonth: day.getMonth() === date.getMonth(),
        isToday: this.isSameDay(day, new Date()),
        isSelected: this.isSameDay(day, date),
        items,
      });
    }
    return cells;
  }

  private buildWeekDays(date: Date): Date[] {
    const start = this.getWeekStart(date);
    return Array.from({ length: 7 }, (_, i) => {
      const day = new Date(start);
      day.setDate(start.getDate() + i);
      return day;
    });
  }

  private getWeekStart(date: Date): Date {
    const day = new Date(date);
    const weekday = (day.getDay() + 6) % 7; // Monday as start
    day.setDate(day.getDate() - weekday);
    day.setHours(0, 0, 0, 0);
    return day;
  }

  private getTodosForDate(date: Date): Todo[] {
    const startOfDay = new Date(date);
    startOfDay.setHours(0, 0, 0, 0);
    const endOfDay = new Date(date);
    endOfDay.setHours(23, 59, 59, 999);

    return this.scheduledTodos().filter((todo) => {
      const start = this.getTodoStart(todo);
      const end = this.getTodoEnd(todo);
      if (!start || !end) return false;
      return start <= endOfDay && end >= startOfDay;
    });
  }

  private isSameDay(a: Date, b: Date): boolean {
    return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
  }

  private showError(msg: string) {
    this.errorMessage.set(msg);
    setTimeout(() => this.errorMessage.set(''), 4000);
  }
}
