export async function bffLogin(username, password){
  const r = await fetch('/bff/login', {
    method: 'POST',
    headers: {'Content-Type':'application/json'},
    credentials: 'include',
    body: JSON.stringify({ username, password })
  })
  if (!r.ok) throw new Error('Invalid credentials')
  return r.json()
}

export async function bffLogout(){
  await fetch('/bff/logout', { method:'POST', credentials: 'include' })
}

// Students
export async function getStudents(){
  const r = await fetch('/bff/students', { credentials:'include' })
  if (!r.ok) throw new Error('Failed to fetch students')
  return r.json()
}
export async function createStudent(dto){
  const r = await fetch('/bff/students', { method:'POST', headers:{'Content-Type':'application/json'}, credentials:'include', body: JSON.stringify(dto) })
  if (!r.ok) throw new Error('Failed to create student')
  return r.json()
}
export async function updateStudent(id, dto){
  const r = await fetch(`/bff/students/${id}`, { method:'PUT', headers:{'Content-Type':'application/json'}, credentials:'include', body: JSON.stringify(dto) })
  if (!r.ok) throw new Error('Failed to update student')
}
export async function deleteStudent(id){
  const r = await fetch(`/bff/students/${id}`, { method:'DELETE', credentials:'include' })
  if (!r.ok) throw new Error('Failed to delete student')
}

// Teachers
export async function getTeachers(){
  const r = await fetch('/bff/teachers', { credentials:'include' })
  if (!r.ok) throw new Error('Failed to fetch teachers')
  return r.json()
}
export async function createTeacher(dto){
  const r = await fetch('/bff/teachers', { method:'POST', headers:{'Content-Type':'application/json'}, credentials:'include', body: JSON.stringify(dto) })
  if (!r.ok) throw new Error('Failed to create teacher')
  return r.json()
}
export async function updateTeacher(id, dto){
  const r = await fetch(`/bff/teachers/${id}`, { method:'PUT', headers:{'Content-Type':'application/json'}, credentials:'include', body: JSON.stringify(dto) })
  if (!r.ok) throw new Error('Failed to update teacher')
}
export async function deleteTeacher(id){
  const r = await fetch(`/bff/teachers/${id}`, { method:'DELETE', credentials:'include' })
  if (!r.ok) throw new Error('Failed to delete teacher')
}
